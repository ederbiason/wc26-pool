using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.DTOs;
using WC26Pool.API.Models;
using WC26Pool.API.Helpers;
using WC26Pool.API.Services;

namespace WC26Pool.API.Endpoints;

public static class PredictionEndpoints
{
    public static void MapPredictionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/predictions");

        group.MapPost("/", async (
            CreatePredictionRequest request,
            AppDbContext db) =>
        {
            var participantId = request.ParticipantId;

            var match = await db.Matches.FindAsync(request.MatchId);
            if (match is null)
                return Results.NotFound("Match not found");

            if (match.Status != MatchStatus.NotStarted)
                return Results.UnprocessableEntity($"This match has already {match.Status.ToString().ToLower()} — predictions are closed");

            var participant = await db.Participants.FindAsync(participantId);
            if (participant is null)
                return Results.NotFound("Participant not found");

            var existingPrediction = await db.Predictions
                .FirstOrDefaultAsync(p => p.ParticipantId == participantId && p.MatchId == request.MatchId);

            if (existingPrediction is not null)
                return Results.Conflict("Prediction already exists for this match");

            var prediction = new Prediction
            {
                ParticipantId = participantId,
                MatchId = request.MatchId,
                PredictedHomeScore = request.PredictedHomeScore,
                PredictedAwayScore = request.PredictedAwayScore,
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.Predictions.Add(prediction);
            await db.SaveChangesAsync();

            return Results.Created($"/api/predictions/{prediction.Id}", new { prediction.Id });
        });

        group.MapGet("/day/{date}", async (
            string date,
            HttpContext httpContext,
            AppDbContext db,
            PredictionVisibilityService visibilityService) =>
        {
            if (!DateOnly.TryParse(date, out var parsedDate))
                return Results.BadRequest("Invalid date format. Use yyyy-MM-dd");

            var participantId = ParseParticipantId(httpContext);

            var (startUtc, endUtc) = BrasiliaTime.DisplayDayUtcBounds(parsedDate);

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


    }

    private static int? ParseParticipantId(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Participant-Id", out var value) &&
            int.TryParse(value, out var id))
            return id;
        return null;
    }
}
