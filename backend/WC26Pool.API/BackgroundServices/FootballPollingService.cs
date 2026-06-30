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
            await SyncMatchesAsync(db, apiMatches, scoringService: null, cancellationToken);

        _lastUpcomingSync = today;
        logger.LogInformation("Upcoming sync complete — {Count} matches from {From} to {To}", apiMatches.Count, from, to);
    }

    private async Task<TimeSpan> ProcessTodayAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var footballApi = scope.ServiceProvider.GetRequiredService<FootballApiService>();
        var scoringService = scope.ServiceProvider.GetRequiredService<ScoringService>();

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
                    await SyncMatchesAsync(db, overdueApiMatches, scoringService, cancellationToken);
            }
            // --------------------------------------------------------------------------

            var apiMatches = await footballApi.GetMatchesForDateAsync(today, cancellationToken);

            if (apiMatches.Count > 0)
            {
                await SyncMatchesAsync(db, apiMatches, scoringService, cancellationToken);
            }

            var dbMatches = await db.Matches
                .Where(m => m.MatchDate.UtcDateTime >= todayStartUtc && m.MatchDate.UtcDateTime < todayEndUtc)
                .OrderBy(m => m.MatchDate)
                .ToListAsync(cancellationToken);

            if (dbMatches.Count == 0 || dbMatches.All(m => m.Status == MatchStatus.Finished))
                return TimeUntilMidnight(nowUtc);

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

                if (previousStatus != MatchStatus.Finished && newStatus == MatchStatus.Finished && scoringService is not null)
                {
                    await db.SaveChangesAsync(cancellationToken);
                    await scoringService.CalculatePointsForMatchAsync(existing.Id);
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
    private static (int? Home, int? Away) ResolveMatchScore(FootballApiScore? score)
    {
        if (score is null)
            return (null, null);

        return score.Duration switch
        {
            "REGULAR" => (score.FullTime?.Home, score.FullTime?.Away),

            "EXTRA_TIME" or "PENALTY_SHOOTOUT" => (
                (score.RegularTime?.Home ?? 0) + (score.ExtraTime?.Home ?? 0),
                (score.RegularTime?.Away ?? 0) + (score.ExtraTime?.Away ?? 0)
            ),

            _ => (score.FullTime?.Home, score.FullTime?.Away)
        };
    }
}
