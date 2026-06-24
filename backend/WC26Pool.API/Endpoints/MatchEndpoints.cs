using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.DTOs;
using WC26Pool.API.Models;
using WC26Pool.API.Services;

namespace WC26Pool.API.Endpoints;

public static class MatchEndpoints
{
    public static void MapMatchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/matches");

        group.MapGet("/today", async (HttpContext httpContext, AppDbContext db, PredictionVisibilityService visibilityService) =>
        {
            var participantId = ParseParticipantId(httpContext);
            var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);

            var matches = await db.Matches
                .AsNoTracking()
                .Where(m => DateOnly.FromDateTime(m.MatchDate.Date) == today)
                .OrderBy(m => m.MatchDate)
                .ToListAsync();

            var result = new List<MatchWithVisibilityDto>();
            foreach (var match in matches)
            {
                var visibility = await visibilityService.GetVisibilityForMatchAsync(match.Id, match.Status, participantId);
                result.Add(MatchWithVisibilityDto.FromMatch(match, visibility));
            }

            return Results.Ok(result);
        });

        group.MapGet("/upcoming", async (AppDbContext db) =>
        {
            var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
            var from = today.AddDays(1);
            var to = today.AddDays(7);

            var matches = await db.Matches
                .AsNoTracking()
                .Where(m => DateOnly.FromDateTime(m.MatchDate.Date) >= from &&
                            DateOnly.FromDateTime(m.MatchDate.Date) <= to)
                .OrderBy(m => m.MatchDate)
                .ToListAsync();

            var grouped = matches
                .GroupBy(m => DateOnly.FromDateTime(m.MatchDate.Date))
                .OrderBy(g => g.Key)
                .Select(g => new UpcomingDayDto(
                    g.Key.ToString("yyyy-MM-dd"),
                    g.Select(MatchDto.FromMatch).ToList()))
                .ToList();

            return Results.Ok(grouped);
        });

        group.MapGet("/{id:int}", async (int id, HttpContext httpContext, AppDbContext db, PredictionVisibilityService visibilityService) =>
        {
            var participantId = ParseParticipantId(httpContext);

            var match = await db.Matches
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match is null)
                return Results.NotFound();

            var visibility = await visibilityService.GetVisibilityForMatchAsync(match.Id, match.Status, participantId);
            return Results.Ok(MatchWithVisibilityDto.FromMatch(match, visibility));
        });
    }

    private static int? ParseParticipantId(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Participant-Id", out var value) &&
            int.TryParse(value, out var id))
            return id;
        return null;
    }
}
