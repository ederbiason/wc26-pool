using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.Services;

namespace WC26Pool.API.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin");

        group.MapPost("/reveal/{date}", async (
            string date,
            HttpContext httpContext,
            AppDbContext db,
            PredictionVisibilityService visibilityService) =>
        {
            if (!httpContext.Request.Headers.TryGetValue("X-Participant-Id", out var participantIdHeader) ||
                !int.TryParse(participantIdHeader, out var participantId))
            {
                return Results.Unauthorized();
            }

            var participant = await db.Participants.FindAsync(participantId);
            if (participant is null || !participant.IsAdmin)
                return Results.Forbid();

            if (!DateOnly.TryParse(date, out var parsedDate))
                return Results.BadRequest("Invalid date format. Use yyyy-MM-dd");

            await visibilityService.ForceRevealAsync(parsedDate);

            return Results.Ok(new { message = $"Predictions for {parsedDate} revealed" });
        });
    }
}
