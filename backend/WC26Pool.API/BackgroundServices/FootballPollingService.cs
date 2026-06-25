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
        // On startup: immediately sync the next 7 days before entering the loop
        await RunScopedAsync(async (db, footballApi, scoringService, orderService) =>
        {
            await SyncUpcomingIfNeededAsync(db, footballApi, stoppingToken);
        }, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = await ProcessTodayAsync(stoppingToken);
            await Task.Delay(delay, stoppingToken);
        }
    }

    // Handles today's live score updates + scheduling the daily upcoming sync at midnight
    private async Task<TimeSpan> ProcessTodayAsync(CancellationToken cancellationToken)
    {
        return await RunScopedAsync<TimeSpan>(async (db, footballApi, scoringService, orderService) =>
        {
            var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
            var nowUtc = DateTimeOffset.UtcNow;

            try
            {
                // Daily midnight sync of upcoming 7 days
                await SyncUpcomingIfNeededAsync(db, footballApi, cancellationToken);

                // Today's live data refresh
                var apiMatches = await footballApi.GetMatchesForDateAsync(today, cancellationToken);

                if (apiMatches.Count > 0)
                {
                    await SyncMatchesAsync(db, apiMatches, scoringService, cancellationToken);
                    await orderService.GenerateOrderForDateAsync(today);
                }

                var dbMatches = await db.Matches
                    .Where(m => m.MatchDate.Date == today.ToDateTime(TimeOnly.MinValue))
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

                var minutesUntilNext = (nextMatch.MatchDate - nowUtc).TotalMinutes;

                return minutesUntilNext switch
                {
                    < 10 => TimeSpan.FromSeconds(30),
                    < 30 => TimeSpan.FromMinutes(2),
                    < 120 => TimeSpan.FromMinutes(10),
                    _ => TimeSpan.FromMinutes(20)
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in FootballPollingService, backing off for 2 minutes");
                return TimeSpan.FromMinutes(2);
            }
        }, cancellationToken);
    }

    private async Task SyncUpcomingIfNeededAsync(
        AppDbContext db,
        FootballApiService footballApi,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);

        if (_lastUpcomingSync == today)
            return;

        var from = today.AddDays(1);
        var to = today.AddDays(7);

        logger.LogInformation("Syncing upcoming matches from {From} to {To}", from, to);

        var apiMatches = await footballApi.GetMatchesForRangeAsync(from, to, cancellationToken);

        if (apiMatches.Count > 0)
            await SyncMatchesAsync(db, apiMatches, scoringService: null, cancellationToken);

        _lastUpcomingSync = today;
        logger.LogInformation("Upcoming sync complete — {Count} matches persisted for next 7 days", apiMatches.Count);
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
                    HomeTeam = apiMatch.HomeTeam?.Name ?? "A definir",
                    AwayTeam = apiMatch.AwayTeam?.Name ?? "A definir",
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
                        "Match {ExternalId} ({Home} vs {Away}): {Previous} → {New}",
                        externalId, existing.HomeTeam, existing.AwayTeam, previousStatus, newStatus);
                }

                existing.Status = newStatus;
                existing.HomeScore = apiMatch.Score?.FullTime?.Home;
                existing.AwayScore = apiMatch.Score?.FullTime?.Away;

                // Update team names in case they were "A definir" and now the API has real names
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
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    // Helper to create a scoped DI context, resolve services and run an async action
    private async Task RunScopedAsync(
        Func<AppDbContext, FootballApiService, ScoringService, PredictionOrderService, Task> action,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var footballApi = scope.ServiceProvider.GetRequiredService<FootballApiService>();
        var scoringService = scope.ServiceProvider.GetRequiredService<ScoringService>();
        var orderService = scope.ServiceProvider.GetRequiredService<PredictionOrderService>();
        await action(db, footballApi, scoringService, orderService);
    }

    private async Task<T> RunScopedAsync<T>(
        Func<AppDbContext, FootballApiService, ScoringService, PredictionOrderService, Task<T>> action,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var footballApi = scope.ServiceProvider.GetRequiredService<FootballApiService>();
        var scoringService = scope.ServiceProvider.GetRequiredService<ScoringService>();
        var orderService = scope.ServiceProvider.GetRequiredService<PredictionOrderService>();
        return await action(db, footballApi, scoringService, orderService);
    }

    private static TimeSpan TimeUntilMidnight(DateTimeOffset now)
    {
        var midnight = now.Date.AddDays(1);
        return midnight - now.DateTime;
    }
}
