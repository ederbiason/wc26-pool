using WC26Pool.API.BackgroundServices;

namespace WC26Pool.API.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin");

        // Force an immediate sync of upcoming matches from football-data.org
        group.MapPost("/sync-upcoming", async (FootballPollingService pollingService) =>
        {
            await pollingService.SyncUpcomingMatchesAsync();
            return Results.Ok(new { message = "Upcoming matches sync triggered successfully" });
        });
    }
}
