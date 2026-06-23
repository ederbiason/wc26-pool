using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.DTOs;
using WC26Pool.API.Models;

namespace WC26Pool.API.Endpoints;

public static class MatchEndpoints
{
    public static void MapMatchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/matches");

        group.MapGet("/today", async (AppDbContext db) =>
        {
            var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
            var matches = await db.Matches
                .AsNoTracking()
                .Where(m => DateOnly.FromDateTime(m.MatchDate.Date) == today)
                .OrderBy(m => m.MatchDate)
                .Select(m => MatchDto.FromMatch(m))
                .ToListAsync();

            return Results.Ok(matches);
        });

        group.MapGet("/upcoming", async (AppDbContext db) =>
        {
            var now = DateTimeOffset.UtcNow;
            var matches = await db.Matches
                .AsNoTracking()
                .Where(m => m.MatchDate > now && m.Status == MatchStatus.NotStarted)
                .OrderBy(m => m.MatchDate)
                .Take(20)
                .Select(m => MatchDto.FromMatch(m))
                .ToListAsync();

            return Results.Ok(matches);
        });

        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var match = await db.Matches
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return match is null
                ? Results.NotFound()
                : Results.Ok(MatchDto.FromMatch(match));
        });
    }
}
