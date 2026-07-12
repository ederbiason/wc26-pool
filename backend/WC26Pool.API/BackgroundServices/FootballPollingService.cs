using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.Models;
using WC26Pool.API.Services;

namespace WC26Pool.API.BackgroundServices;

public class FootballPollingService(
    IServiceProvider services,
    ILogger<FootballPollingService> logger) : BackgroundService
{
    private DateOnly _lastUpcomingSync = DateOnly.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Sync next 7 days immediately on startup
        await SyncUpcomingMatchesAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = await ProcessTodayAsync(stoppingToken);
            await Task.Delay(delay, stoppingToken);
        }
    }

    // Public so the admin endpoint can trigger it manually
    public async Task SyncUpcomingMatchesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);

        if (_lastUpcomingSync == today)
        {
            logger.LogInformation("Upcoming sync already done today, skipping");
            return;
        }

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var footballApi = scope.ServiceProvider.GetRequiredService<FootballApiService>();

        var from = today.AddDays(1);
        var to = today.AddDays(7);

        logger.LogInformation("Syncing upcoming matches from {From} to {To}", from, to);

        var apiMatches = await footballApi.GetMatchesForRangeAsync(from, to, cancellationToken);

        logger.LogInformation("API returned {Count} upcoming matches", apiMatches.Count);

        if (apiMatches.Count > 0)
            await SyncMatchesAsync(db, apiMatches, scoringService: null, pickemScoringService: null, cancellationToken);

        _lastUpcomingSync = today;
        logger.LogInformation("Upcoming sync complete — {Count} matches from {From} to {To}", apiMatches.Count, from, to);
    }

    private async Task<TimeSpan> ProcessTodayAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var footballApi = scope.ServiceProvider.GetRequiredService<FootballApiService>();
        var scoringService = scope.ServiceProvider.GetRequiredService<ScoringService>();
        var pickemScoringService = scope.ServiceProvider.GetRequiredService<PickemScoringService>();

        var nowUtc = DateTime.UtcNow;
        var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        var nowBrasilia = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, brasiliaZone);
        var todayStartLocal = new DateTime(nowBrasilia.Year, nowBrasilia.Month, nowBrasilia.Day,
                                           0, 0, 0, DateTimeKind.Unspecified);
        var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(todayStartLocal, brasiliaZone);
        var todayEndUtc   = todayStartUtc.AddDays(1);
        var today = DateOnly.FromDateTime(nowBrasilia.Date);

        try
        {
            // Re-sync upcoming at midnight (Brasília)
            if (_lastUpcomingSync != today)
                await SyncUpcomingMatchesAsync(cancellationToken);

            // --- Overdue check: NotStarted match whose kickoff time has already passed ---
            var overdueMatches = await db.Matches
                .Where(m => m.Status == MatchStatus.NotStarted && m.MatchDate.UtcDateTime <= nowUtc)
                .ToListAsync(cancellationToken);

            if (overdueMatches.Count > 0)
            {
                logger.LogWarning(
                    "{Count} overdue match(es) detected (NotStarted but kickoff passed). Fetching today immediately.",
                    overdueMatches.Count);

                // Fetch today's full data to resolve overdue statuses
                var overdueApiMatches = await footballApi.GetMatchesForDateAsync(today, cancellationToken);
                if (overdueApiMatches.Count > 0)
                    await SyncMatchesAsync(db, overdueApiMatches, scoringService, pickemScoringService, cancellationToken);
            }
            // --------------------------------------------------------------------------

            var apiMatches = await footballApi.GetMatchesForDateAsync(today, cancellationToken);

            if (apiMatches.Count > 0)
            {
                await SyncMatchesAsync(db, apiMatches, scoringService, pickemScoringService, cancellationToken);
            }

            var dbMatches = await db.Matches
                .Where(m => m.MatchDate.UtcDateTime >= todayStartUtc && m.MatchDate.UtcDateTime < todayEndUtc)
                .OrderBy(m => m.MatchDate)
                .ToListAsync(cancellationToken);

            var hasPendingConfirmation = dbMatches.Any(m =>
                m.Status == MatchStatus.Finished &&
                !m.PointsCalculated &&
                m.FinishedDetectedAt != null);

            if (!hasPendingConfirmation &&
                (dbMatches.Count == 0 || dbMatches.All(m => m.Status == MatchStatus.Finished)))
                return TimeUntilMidnight(nowUtc);

            if (hasPendingConfirmation && dbMatches.All(m => m.Status == MatchStatus.Finished))
                return TimeSpan.FromMinutes(1);

            if (dbMatches.Any(m => m.Status == MatchStatus.InProgress))
                return TimeSpan.FromMinutes(1);

            var nextMatch = dbMatches
                .Where(m => m.Status == MatchStatus.NotStarted)
                .OrderBy(m => m.MatchDate)
                .FirstOrDefault();

            if (nextMatch is null)
                return TimeUntilMidnight(nowUtc);

            var minutesUntilNext = (nextMatch.MatchDate.UtcDateTime - nowUtc).TotalMinutes;

            return minutesUntilNext switch
            {
                <= 0 => TimeSpan.FromMinutes(1),
                < 30 => TimeSpan.FromMinutes(2),
                < 120 => TimeSpan.FromMinutes(10),
                _ => TimeSpan.FromMinutes(20)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in FootballPollingService, backing off 2 minutes");
            return TimeSpan.FromMinutes(2);
        }
    }

    private async Task SyncMatchesAsync(
        AppDbContext db,
        List<FootballApiMatch> apiMatches,
        ScoringService? scoringService,
        PickemScoringService? pickemScoringService,
        CancellationToken cancellationToken)
    {
        foreach (var apiMatch in apiMatches)
        {
            var externalId = apiMatch.Id.ToString();
            var newStatus = FootballApiMatchStatusMapper.MapStatus(apiMatch.Status);
            var duration = apiMatch.Score?.Duration ?? "REGULAR";
            var stage = apiMatch.Stage switch
            {
                "LAST_32" or "LAST_16" or "QUARTER_FINALS" or "SEMI_FINALS" or "THIRD_PLACE" or "FINAL" => apiMatch.Stage,
                _ => "GROUP_STAGE"
            };
            var groupName = apiMatch.Group;

            var existing = await db.Matches
                .FirstOrDefaultAsync(m => m.ExternalId == externalId, cancellationToken);

            if (existing is null)
            {
                var (newHome, newAway) = ResolveMatchScore(apiMatch.Score);
                db.Matches.Add(new Match
                {
                    ExternalId = externalId,
                    HomeTeam = apiMatch.HomeTeam?.Name ?? "A definir",
                    AwayTeam = apiMatch.AwayTeam?.Name ?? "A definir",
                    HomeTeamFlag = apiMatch.HomeTeam?.Crest ?? string.Empty,
                    AwayTeamFlag = apiMatch.AwayTeam?.Crest ?? string.Empty,
                    MatchDate = DateTimeOffset.Parse(apiMatch.UtcDate),
                    Status = newStatus,
                    HomeScore = newHome,
                    AwayScore = newAway,
                    Stage = stage,
                    GroupName = groupName,
                    Duration = duration,
                    RegularTimeHomeScore = apiMatch.Score?.RegularTime?.Home,
                    RegularTimeAwayScore = apiMatch.Score?.RegularTime?.Away,
                    PenaltyHomeScore = apiMatch.Score?.Penalties?.Home,
                    PenaltyAwayScore = apiMatch.Score?.Penalties?.Away,
                    PointsCalculated = false
                });
            }
            else
            {
                var previousStatus = existing.Status;

                var oldHomeScore = existing.HomeScore;
                var oldAwayScore = existing.AwayScore;

                var (updatedHomeScore, updatedAwayScore) = ResolveMatchScore(apiMatch.Score);
                var updatedDuration = duration;
                var updatedPenaltyHomeScore = apiMatch.Score?.Penalties?.Home;
                var updatedPenaltyAwayScore = apiMatch.Score?.Penalties?.Away;

                // Guard against regressive score updates during live matches.
                // During EXTRA_TIME the API can return extraTime with partial goals before
                // regularTime is populated, causing ResolveMatchScore to produce a lower total.
                if (newStatus == MatchStatus.InProgress)
                {
                    var currentHome = existing.HomeScore ?? 0;
                    var currentAway = existing.AwayScore ?? 0;
                    var newHome = updatedHomeScore ?? 0;
                    var newAway = updatedAwayScore ?? 0;

                    if (newHome + newAway < currentHome + currentAway)
                    {
                        logger.LogWarning(
                            "Match {Id} ({Home} vs {Away}): ignoring regressive score " +
                            "({CurrentHome}-{CurrentAway} → {NewHome}-{NewAway}) during InProgress. " +
                            "API may be returning partial extra time data.",
                            existing.Id, existing.HomeTeam, existing.AwayTeam,
                            currentHome, currentAway, newHome, newAway);

                        updatedHomeScore = existing.HomeScore;
                        updatedAwayScore = existing.AwayScore;
                    }
                }

                var scoreChanged = existing.HomeScore != updatedHomeScore
                    || existing.AwayScore != updatedAwayScore
                    || existing.Duration != updatedDuration
                    || existing.PenaltyHomeScore != updatedPenaltyHomeScore
                    || existing.PenaltyAwayScore != updatedPenaltyAwayScore;

                if (previousStatus != newStatus)
                {
                    logger.LogWarning(
                        "Match {ExternalId} ({Home} vs {Away}): {Previous} → {New}",
                        externalId, existing.HomeTeam, existing.AwayTeam, previousStatus, newStatus);
                }

                existing.Status = newStatus;
                existing.HomeScore = updatedHomeScore;
                existing.AwayScore = updatedAwayScore;
                existing.Stage = stage;
                existing.GroupName = groupName;
                existing.Duration = updatedDuration;
                existing.RegularTimeHomeScore = apiMatch.Score?.RegularTime?.Home;
                existing.RegularTimeAwayScore = apiMatch.Score?.RegularTime?.Away;
                existing.PenaltyHomeScore = updatedPenaltyHomeScore;
                existing.PenaltyAwayScore = updatedPenaltyAwayScore;

                // Update team names when API resolves previously unknown teams
                if (existing.HomeTeam == "A definir" && apiMatch.HomeTeam?.Name is { } homeName)
                    existing.HomeTeam = homeName;
                if (existing.AwayTeam == "A definir" && apiMatch.AwayTeam?.Name is { } awayName)
                    existing.AwayTeam = awayName;
                if (string.IsNullOrEmpty(existing.HomeTeamFlag) && apiMatch.HomeTeam?.Crest is { } homeFlag)
                    existing.HomeTeamFlag = homeFlag;
                if (string.IsNullOrEmpty(existing.AwayTeamFlag) && apiMatch.AwayTeam?.Crest is { } awayFlag)
                    existing.AwayTeamFlag = awayFlag;

                // Block 1: Finished → InProgress (API corrected — reset finish detection)
                if (previousStatus == MatchStatus.Finished && newStatus == MatchStatus.InProgress)
                {
                    existing.FinishedDetectedAt = null;
                    existing.PointsCalculated = false;
                    logger.LogWarning(
                        "Match {Id} ({Home} vs {Away}) reverted from Finished to InProgress. Resetting finish detection.",
                        existing.Id, existing.HomeTeam, existing.AwayTeam);
                }

                // Block 2: InProgress → Finished (with 2-minute confirmation delay)
                if (previousStatus != MatchStatus.Finished && newStatus == MatchStatus.Finished
                    && scoringService is not null)
                {
                    var scoreStable = oldHomeScore == updatedHomeScore
                        && oldAwayScore == updatedAwayScore
                        && existing.Duration == updatedDuration
                        && existing.PenaltyHomeScore == updatedPenaltyHomeScore
                        && existing.PenaltyAwayScore == updatedPenaltyAwayScore;

                    if (!scoreStable)
                    {
                        logger.LogWarning(
                            "Match {Id} ({Home} vs {Away}) finished with score change. Waiting next cycle.",
                            existing.Id, existing.HomeTeam, existing.AwayTeam);

                        existing.FinishedDetectedAt = null;
                        await db.SaveChangesAsync(cancellationToken);
                        continue;
                    }

                    if (existing.FinishedDetectedAt is null)
                    {
                        logger.LogWarning(
                            "Match {Id} ({Home} vs {Away}) detected as Finished for first time. Waiting 2 minutes to confirm.",
                            existing.Id, existing.HomeTeam, existing.AwayTeam);

                        existing.FinishedDetectedAt = DateTimeOffset.UtcNow;
                        await db.SaveChangesAsync(cancellationToken);
                        continue;
                    }

                    var minutesSinceFinished = (DateTimeOffset.UtcNow - existing.FinishedDetectedAt.Value).TotalMinutes;

                    if (minutesSinceFinished < 2)
                    {
                        logger.LogInformation(
                            "Match {Id} waiting for finish confirmation ({Minutes:F1} min elapsed, need 2).",
                            existing.Id, minutesSinceFinished);

                        await db.SaveChangesAsync(cancellationToken);
                        continue;
                    }

                    logger.LogInformation(
                        "Match {Id} ({Home} vs {Away}) confirmed Finished after {Minutes:F1} minutes. Calculating points.",
                        existing.Id, existing.HomeTeam, existing.AwayTeam, minutesSinceFinished);

                    await db.SaveChangesAsync(cancellationToken);
                    await scoringService.CalculatePointsForMatchAsync(existing.Id);
                    if (pickemScoringService is not null)
                        await pickemScoringService.CalculatePickemPointsForMatchAsync(existing.Id);
                    continue;
                }

                if (previousStatus == MatchStatus.Finished && newStatus == MatchStatus.Finished
                    && !existing.PointsCalculated && scoringService is not null)
                {
                    var scoreStable = oldHomeScore == updatedHomeScore
                        && oldAwayScore == updatedAwayScore
                        && existing.Duration == updatedDuration
                        && existing.PenaltyHomeScore == updatedPenaltyHomeScore
                        && existing.PenaltyAwayScore == updatedPenaltyAwayScore;

                    if (scoreStable)
                    {
                        logger.LogInformation(
                            "Match {Id} ({Home} vs {Away}) score confirmed stable ({NewHome}-{NewAway}). Calculating points.",
                            existing.Id, existing.HomeTeam, existing.AwayTeam, updatedHomeScore, updatedAwayScore);

                        await db.SaveChangesAsync(cancellationToken);
                        await scoringService.CalculatePointsForMatchAsync(existing.Id);
                        if (pickemScoringService is not null)
                            await pickemScoringService.CalculatePickemPointsForMatchAsync(existing.Id);
                        continue;
                    }

                    logger.LogWarning(
                        "Match {Id} ({Home} vs {Away}) score still unstable ({OldHome}-{OldAway} \u2192 {NewHome}-{NewAway}). Waiting another cycle.",
                        existing.Id, existing.HomeTeam, existing.AwayTeam,
                        oldHomeScore, oldAwayScore, updatedHomeScore, updatedAwayScore);

                    await db.SaveChangesAsync(cancellationToken);
                    continue;
                }

                if (previousStatus == MatchStatus.Finished && newStatus == MatchStatus.Finished && scoreChanged && existing.PointsCalculated && scoringService is not null)
                {
                    logger.LogWarning(
                        "Score correction detected for match {Id}: {OldScore} → {NewScore}. Recalculating points.",
                        existing.Id, $"{oldHomeScore}-{oldAwayScore}", $"{updatedHomeScore}-{updatedAwayScore}");

                    existing.PointsCalculated = false;

                    var predictions = await db.Predictions
                        .Include(p => p.Participant)
                        .Where(p => p.MatchId == existing.Id)
                        .ToListAsync(cancellationToken);

                    foreach (var prediction in predictions)
                    {
                        if (prediction.PointsEarned.HasValue)
                        {
                            prediction.Participant.TotalPoints -= prediction.PointsEarned.Value;
                            prediction.PointsEarned = null;
                        }
                    }

                    await db.SaveChangesAsync(cancellationToken);
                    await scoringService.CalculatePointsForMatchAsync(existing.Id);
                    continue;
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static TimeSpan TimeUntilMidnight(DateTimeOffset now)
    {
        var midnight = now.Date.AddDays(1);
        return midnight - now.DateTime;
    }

    /// Resolves HomeScore/AwayScore for a match based on the duration field.
    /// For REGULAR duration, fullTime is the authoritative result (regularTime is absent in the API response).
    /// For EXTRA_TIME or PENALTY_SHOOTOUT, HomeScore/AwayScore should reflect goals through regular+extra time
    /// only (penalties are stored separately in PenaltyHomeScore/AwayScore).
    internal static (int? Home, int? Away) ResolveMatchScore(FootballApiScore? score)
    {
        if (score is null)
            return (null, null);

        return score.Duration switch
        {
            "REGULAR" => (score.FullTime?.Home, score.FullTime?.Away),

            // When regularTime is null/empty (API sends partial data during live EXTRA_TIME),
            // fall back to fullTime which is always the authoritative running total.
            "EXTRA_TIME" => (
                score.RegularTime?.Home is not null
                    ? (score.RegularTime.Home ?? 0) + (score.ExtraTime?.Home ?? 0)
                    : score.FullTime?.Home,
                score.RegularTime?.Away is not null
                    ? (score.RegularTime.Away ?? 0) + (score.ExtraTime?.Away ?? 0)
                    : score.FullTime?.Away
            ),

            // PENALTY_SHOOTOUT: regularTime is always populated; exclude penalty goals.
            "PENALTY_SHOOTOUT" => (
                (score.RegularTime?.Home ?? 0) + (score.ExtraTime?.Home ?? 0),
                (score.RegularTime?.Away ?? 0) + (score.ExtraTime?.Away ?? 0)
            ),

            _ => (score.FullTime?.Home, score.FullTime?.Away)
        };
    }
}
