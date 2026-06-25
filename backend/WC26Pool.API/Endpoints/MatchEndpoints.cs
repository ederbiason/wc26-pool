using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.DTOs;
using WC26Pool.API.Models;
using WC26Pool.API.Services;

namespace WC26Pool.API.Endpoints;

internal static class BrasiliaTime
{
    private static readonly TimeZoneInfo Zone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    /// Returns (startUtc, endUtc) for the current day in Brasília time.
    public static (DateTime startUtc, DateTime endUtc) TodayUtcBounds()
    {
        var nowBrasilia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);
        var startLocal = new DateTime(nowBrasilia.Year, nowBrasilia.Month, nowBrasilia.Day,
                                      0, 0, 0, DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, Zone);
        return (startUtc, startUtc.AddDays(1));
    }

    /// Returns the UTC boundaries for the window [today+daysFrom, today+daysTo) in Brasília.
    public static (DateTime startUtc, DateTime endUtc) OffsetUtcBounds(int daysFrom, int daysTo)
    {
        var nowBrasilia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);
        var baseLocal = new DateTime(nowBrasilia.Year, nowBrasilia.Month, nowBrasilia.Day,
                                     0, 0, 0, DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(baseLocal.AddDays(daysFrom), Zone);
        var endUtc   = TimeZoneInfo.ConvertTimeToUtc(baseLocal.AddDays(daysTo),   Zone);
        return (startUtc, endUtc);
    }
}

public static class MatchEndpoints
{
    public static void MapMatchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/matches");

        group.MapGet("/today", async (HttpContext httpContext, AppDbContext db, PredictionVisibilityService visibilityService) =>
        {
            var participantId = ParseParticipantId(httpContext);
            var (startUtc, endUtc) = BrasiliaTime.TodayUtcBounds();

            var matches = await db.Matches
                .AsNoTracking()
                .Where(m => m.MatchDate >= startUtc && m.MatchDate < endUtc)
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
            // "Tomorrow" through "D+7" in Brasília timezone
            var (fromUtc, toUtc) = BrasiliaTime.OffsetUtcBounds(daysFrom: 1, daysTo: 8);

            var matches = await db.Matches
                .AsNoTracking()
                .Where(m => m.MatchDate >= fromUtc && m.MatchDate < toUtc)
                .OrderBy(m => m.MatchDate)
                .ToListAsync();

            var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            var grouped = matches
                .GroupBy(m => DateOnly.FromDateTime(
                    TimeZoneInfo.ConvertTimeFromUtc(m.MatchDate.UtcDateTime, brasiliaZone).Date))
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
