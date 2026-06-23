using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.Models;
using WC26Pool.API.Services;

namespace WC26Pool.API.BackgroundServices;

public class FootballPollingService(
    IServiceProvider services,
    ILogger<FootballPollingService> logger) : BackgroundService
{
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
            var apiMatches = await footballApi.GetMatchesForDateAsync(today, cancellationToken);

            if (apiMatches.Count == 0)
                return TimeUntilMidnight(nowUtc);

            await SyncMatchesAsync(db, apiMatches, scoringService, cancellationToken);
            await orderService.GenerateOrderForDateAsync(today);

            var dbMatches = await db.Matches
                .Where(m => DateOnly.FromDateTime(m.MatchDate.Date) == today)
                .OrderBy(m => m.MatchDate)
                .ToListAsync(cancellationToken);

            if (dbMatches.All(m => m.Status == MatchStatus.Finished))
                return TimeUntilMidnight(nowUtc);

            var inProgress = dbMatches.Any(m => m.Status == MatchStatus.InProgress);
            if (inProgress)
                return TimeSpan.FromMinutes(5);

            var nextMatch = dbMatches
                .Where(m => m.Status == MatchStatus.NotStarted)
                .OrderBy(m => m.MatchDate)
                .FirstOrDefault();

            if (nextMatch is null)
                return TimeUntilMidnight(nowUtc);

            var minutesUntilNext = (nextMatch.MatchDate - nowUtc).TotalMinutes;

            if (minutesUntilNext < 30)
                return TimeSpan.FromMinutes(10);

            if (minutesUntilNext < 120)
                return TimeSpan.FromMinutes(30);

            return TimeSpan.FromMinutes(30);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in FootballPollingService, backing off for 5 minutes");
            return TimeSpan.FromMinutes(5);
        }
    }

    private async Task SyncMatchesAsync(
        AppDbContext db,
        List<Services.FootballApiMatch> apiMatches,
        ScoringService scoringService,
        CancellationToken cancellationToken)
    {
        foreach (var apiMatch in apiMatches)
        {
            var externalId = apiMatch.Fixture.Id.ToString();
            var newStatus = FootballApiMatchStatusMapper.MapStatus(apiMatch.Fixture.Status.Short);

            var existing = await db.Matches
                .FirstOrDefaultAsync(m => m.ExternalId == externalId, cancellationToken);

            if (existing is null)
            {
                db.Matches.Add(new Match
                {
                    ExternalId = externalId,
                    HomeTeam = apiMatch.Teams.Home.Name,
                    AwayTeam = apiMatch.Teams.Away.Name,
                    HomeTeamFlag = apiMatch.Teams.Home.Logo,
                    AwayTeamFlag = apiMatch.Teams.Away.Logo,
                    MatchDate = DateTimeOffset.Parse(apiMatch.Fixture.Date),
                    Status = newStatus,
                    HomeScore = apiMatch.Goals.Home,
                    AwayScore = apiMatch.Goals.Away,
                    PointsCalculated = false
                });
            }
            else
            {
                var previousStatus = existing.Status;
                existing.Status = newStatus;
                existing.HomeScore = apiMatch.Goals.Home;
                existing.AwayScore = apiMatch.Goals.Away;

                if (previousStatus != MatchStatus.Finished && newStatus == MatchStatus.Finished)
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
