using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.DTOs;
using WC26Pool.API.Models;
using WC26Pool.API.Services;

namespace WC26Pool.API.Endpoints;

using WC26Pool.API.Helpers;

public static class MatchEndpoints
{
    public static void MapMatchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/matches");

        group.MapGet("/today", async (HttpContext httpContext, AppDbContext db, PredictionVisibilityService visibilityService) =>
        {
            var participantId = ParseParticipantId(httpContext);
            var today = BrasiliaTime.GetTodayDisplayDay();
            return Results.Ok(await GetMatchesForDayAsync(db, visibilityService, today, participantId));
        });

        group.MapGet("/upcoming", async (AppDbContext db) =>
        {
            // "Tomorrow" through "D+7" in Brasília timezone
            var (fromUtc, toUtc) = BrasiliaTime.OffsetUtcBounds(daysFrom: 1, daysTo: 8);

            var matches = await db.Matches
                .AsNoTracking()
                .Where(m => m.MatchDate >= fromUtc && m.MatchDate < toUtc)
                .OrderBy(m => m.MatchDate)
                .ToListAsync();

            var grouped = matches
                .AsEnumerable()
                .GroupBy(m => BrasiliaTime.GetDisplayDay(m.MatchDate))
                .OrderBy(g => g.Key)
                .Select(g => new UpcomingDayDto(
                    g.Key.ToString("yyyy-MM-dd"),
                    g.Select(MatchDto.FromMatch).ToList()))
                .ToList();

            return Results.Ok(grouped);
        });

        group.MapGet("/day/{date}", async (string date, HttpContext httpContext, AppDbContext db, PredictionVisibilityService visibilityService) =>
        {
            if (!DateOnly.TryParse(date, out var parsedDate))
                return Results.BadRequest("Invalid date format. Use yyyy-MM-dd");

            var participantId = ParseParticipantId(httpContext);
            return Results.Ok(await GetMatchesForDayAsync(db, visibilityService, parsedDate, participantId));
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

    private static async Task<List<MatchWithVisibilityDto>> GetMatchesForDayAsync(
        AppDbContext db,
        PredictionVisibilityService visibilityService,
        DateOnly day,
        int? participantId)
    {
        var (startUtc, endUtc) = BrasiliaTime.DisplayDayUtcBounds(day);

        var matches = await db.Matches
            .AsNoTracking()
            .Where(m => m.MatchDate >= startUtc && m.MatchDate < endUtc)
            .OrderBy(m => m.MatchDate)
            .ToListAsync();

        var matchStatuses = matches.ToDictionary(m => m.Id, m => m.Status);
        var visibilityDict = await visibilityService.GetVisibilityForMatchesAsync(matchStatuses, participantId);

        return matches
            .Select(m => MatchWithVisibilityDto.FromMatch(m, visibilityDict[m.Id]))
            .ToList();
    }
}
