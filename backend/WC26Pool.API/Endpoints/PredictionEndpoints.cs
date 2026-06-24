using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.DTOs;
using WC26Pool.API.Models;
using WC26Pool.API.Services;

namespace WC26Pool.API.Endpoints;

public static class PredictionEndpoints
{
    public static void MapPredictionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/predictions");

        group.MapPost("/", async (
            CreatePredictionRequest request,
            AppDbContext db,
            PredictionOrderService orderService) =>
        {
            var participantId = request.ParticipantId;

            var participant = await db.Participants.FindAsync(participantId);
            if (participant is null)
                return Results.NotFound("Participant not found");

            var match = await db.Matches.FindAsync(request.MatchId);
            if (match is null)
                return Results.NotFound("Match not found");

            if (match.Status != MatchStatus.NotStarted)
                return Results.UnprocessableEntity("Cannot predict on a match that has already started or finished");

            var existingPrediction = await db.Predictions
                .FirstOrDefaultAsync(p => p.ParticipantId == participantId && p.MatchId == request.MatchId);

            if (existingPrediction is not null)
                return Results.Conflict("Prediction already exists for this match");

            var matchDate = DateOnly.FromDateTime(match.MatchDate.Date);

            var orderExists = await db.DayPredictionOrders
                .AnyAsync(d => d.Date == matchDate);

            if (!orderExists)
                await orderService.GenerateOrderForDateAsync(matchDate);

            var firstMatchOfDay = await db.Matches
                .Where(m => DateOnly.FromDateTime(m.MatchDate.Date) == matchDate)
                .OrderBy(m => m.MatchDate)
                .FirstOrDefaultAsync();

            if (firstMatchOfDay is null)
                return Results.UnprocessableEntity("No matches found for this day");

            var canPredict = await orderService.CanParticipantPredictAsync(
                participantId, matchDate, firstMatchOfDay.MatchDate);

            if (!canPredict)
                return Results.UnprocessableEntity("You are not allowed to predict yet based on the current order");

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

            await orderService.UpdateSubmissionStatusAsync(participantId, matchDate);

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

            var matches = await db.Matches
                .AsNoTracking()
                .Where(m => DateOnly.FromDateTime(m.MatchDate.Date) == parsedDate)
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

        group.MapGet("/order/{date}", async (string date, AppDbContext db) =>
        {
            if (!DateOnly.TryParse(date, out var parsedDate))
                return Results.BadRequest("Invalid date format. Use yyyy-MM-dd");

            var orders = await db.DayPredictionOrders
                .AsNoTracking()
                .Include(d => d.Participant)
                .Where(d => d.Date == parsedDate)
                .OrderBy(d => d.Order)
                .Select(d => new DayPredictionOrderDto(
                    d.ParticipantId,
                    d.Participant.Name,
                    d.Order,
                    d.HasSubmittedAll))
                .ToListAsync();

            return Results.Ok(orders);
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
