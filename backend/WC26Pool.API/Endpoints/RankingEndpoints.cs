using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.DTOs;

namespace WC26Pool.API.Endpoints;

public static class RankingEndpoints
{
    public static void MapRankingEndpoints(this WebApplication app)
    {
        app.MapGet("/api/ranking", async (AppDbContext db) =>
        {
            var participants = await db.Participants
                .AsNoTracking()
                .OrderByDescending(p => p.TotalPoints)
                .ThenBy(p => p.Name)
                .ToListAsync();

            var ranking = participants
                .Select((p, index) => new RankingEntryDto(index + 1, p.Id, p.Name, p.TotalPoints))
                .ToList();

            return Results.Ok(ranking);
        });
    }
}
