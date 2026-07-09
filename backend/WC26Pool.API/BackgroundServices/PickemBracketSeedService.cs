using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.Models;
using WC26Pool.API.Services;

namespace WC26Pool.API.BackgroundServices;

public class PickemBracketSeedService(
    IServiceProvider services,
    ILogger<PickemBracketSeedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Small delay to let the DB migrate first
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        await SeedBracketAsync(stoppingToken);
    }

    private async Task SeedBracketAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var footballApi = scope.ServiceProvider.GetRequiredService<FootballApiService>();

        var existingCount = await db.PickemBracketSlots.CountAsync(cancellationToken);
        if (existingCount >= 8)
        {
            logger.LogInformation("PickemBracketSlots already seeded ({Count} slots). Skipping.", existingCount);
            return;
        }

        logger.LogInformation("Seeding quarter-final bracket from football-data API...");

        var quarterFinalMatches = await footballApi.GetMatchesByStageAsync("QUARTER_FINALS", cancellationToken);

        if (quarterFinalMatches.Count == 0)
        {
            logger.LogWarning("No QUARTER_FINALS matches returned from API. Bracket seed skipped.");
            return;
        }

        // Order by UtcDate so slots are consistent (earliest match first)
        var ordered = quarterFinalMatches.OrderBy(m => m.UtcDate).ToList();

        var slots = new List<PickemBracketSlot>();
        for (var i = 0; i < ordered.Count && i < 4; i++)
        {
            var match = ordered[i];
            slots.Add(new PickemBracketSlot
            {
                SlotIndex = i * 2,
                TeamName = match.HomeTeam?.Name ?? "A definir",
                TeamFlag = match.HomeTeam?.Crest ?? string.Empty,
            });
            slots.Add(new PickemBracketSlot
            {
                SlotIndex = i * 2 + 1,
                TeamName = match.AwayTeam?.Name ?? "A definir",
                TeamFlag = match.AwayTeam?.Crest ?? string.Empty,
            });
        }

        // Clear any partial data before inserting
        db.PickemBracketSlots.RemoveRange(db.PickemBracketSlots);
        db.PickemBracketSlots.AddRange(slots);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Bracket seeded with {Count} slots.", slots.Count);
    }
}
