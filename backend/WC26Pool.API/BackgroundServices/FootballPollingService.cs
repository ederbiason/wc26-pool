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
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = await ProcessMatchesAsync(stoppingToken);
            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task<TimeSpan> ProcessMatchesAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var footballApi = scope.ServiceProvider.GetRequiredService<FootballApiService>();
        var scoringService = scope.ServiceProvider.GetRequiredService<ScoringService>();
        var orderService = scope.ServiceProvider.GetRequiredService<PredictionOrderService>();

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
        var nowUtc = DateTimeOffset.UtcNow;

        try
        {
            await SyncUpcomingMatchesIfNeededAsync(db, footballApi, today, cancellationToken);

            var apiMatches = await footballApi.GetMatchesForDateAsync(today, cancellationToken);

            if (apiMatches.Count > 0)
            {
                await SyncMatchesAsync(db, apiMatches, scoringService, cancellationToken);
                await orderService.GenerateOrderForDateAsync(today);
            }

            var dbMatches = await db.Matches
                .Where(m => DateOnly.FromDateTime(m.MatchDate.Date) == today)
                .OrderBy(m => m.MatchDate)
                .ToListAsync(cancellationToken);

            if (dbMatches.Count == 0 || dbMatches.All(m => m.Status == MatchStatus.Finished))
                return TimeUntilMidnight(nowUtc);

            var inProgress = dbMatches.Any(m => m.Status == MatchStatus.InProgress);
            if (inProgress)
                return TimeSpan.FromMinutes(1);

            var nextMatch = dbMatches
                .Where(m => m.Status == MatchStatus.NotStarted)
                .OrderBy(m => m.MatchDate)
                .FirstOrDefault();

            if (nextMatch is null)
                return TimeUntilMidnight(nowUtc);

            var minutesUntilNext = (nextMatch.MatchDate - nowUtc).TotalMinutes;

            if (minutesUntilNext < 10)
                return TimeSpan.FromSeconds(30);

            if (minutesUntilNext < 30)
                return TimeSpan.FromMinutes(2);

            if (minutesUntilNext < 120)
                return TimeSpan.FromMinutes(10);

            return TimeSpan.FromMinutes(20);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in FootballPollingService, backing off for 2 minutes");
            return TimeSpan.FromMinutes(2);
        }
    }

    private async Task SyncUpcomingMatchesIfNeededAsync(
        AppDbContext db,
        FootballApiService footballApi,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (_lastUpcomingSync == today)
            return;

        logger.LogInformation("Syncing upcoming matches for next 7 days starting {Today}", today);

        var from = today.AddDays(1);
        var to = today.AddDays(7);

        var apiMatches = await footballApi.GetMatchesForRangeAsync(from, to, cancellationToken);

        if (apiMatches.Count > 0)
            await SyncMatchesAsync(db, apiMatches, scoringService: null, cancellationToken);

        _lastUpcomingSync = today;
        logger.LogInformation("Upcoming sync complete — {Count} matches fetched for next 7 days", apiMatches.Count);
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

            var existing = await db.Matches
                .FirstOrDefaultAsync(m => m.ExternalId == externalId, cancellationToken);

            if (existing is null)
            {
                db.Matches.Add(new Match
                {
                    ExternalId = externalId,
                    HomeTeam = apiMatch.HomeTeam?.Name ?? "TBD",
                    AwayTeam = apiMatch.AwayTeam?.Name ?? "TBD",
                    HomeTeamFlag = apiMatch.HomeTeam?.Crest ?? string.Empty,
                    AwayTeamFlag = apiMatch.AwayTeam?.Crest ?? string.Empty,
                    MatchDate = DateTimeOffset.Parse(apiMatch.UtcDate),
                    Status = newStatus,
                    HomeScore = apiMatch.Score?.FullTime?.Home,
                    AwayScore = apiMatch.Score?.FullTime?.Away,
                    PointsCalculated = false
                });
            }
            else
            {
                var previousStatus = existing.Status;

                if (previousStatus != newStatus)
                {
                    logger.LogWarning(
                        "Match {ExternalId} ({Home} vs {Away}) status transition: {Previous} → {New}",
                        externalId, existing.HomeTeam, existing.AwayTeam, previousStatus, newStatus);
                }

                existing.Status = newStatus;
                existing.HomeScore = apiMatch.Score?.FullTime?.Home;
                existing.AwayScore = apiMatch.Score?.FullTime?.Away;

                if (previousStatus != MatchStatus.Finished && newStatus == MatchStatus.Finished && scoringService is not null)
                {
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
}
