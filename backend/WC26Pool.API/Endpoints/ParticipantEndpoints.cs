using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.DTOs;

namespace WC26Pool.API.Endpoints;

public static class ParticipantEndpoints
{
    public static void MapParticipantEndpoints(this WebApplication app)
    {
        app.MapGet("/api/participants", async (AppDbContext db) =>
        {
            var participants = await db.Participants
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => ParticipantDto.FromParticipant(p))
                .ToListAsync();

            return Results.Ok(participants);
        });
    }
}
