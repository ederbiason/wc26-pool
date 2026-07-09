using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.Models;

namespace WC26Pool.API.Endpoints;

public static class PickemEndpoints
{
    private static readonly DateTimeOffset Deadline =
        new(2026, 7, 9, 20, 0, 0, TimeSpan.FromHours(-3));

    private const int TotalParticipants = 6;

    public static void MapPickemEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/pickem");

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/pickem/bracket
        // ─────────────────────────────────────────────────────────────────────
        group.MapGet("/bracket", async (AppDbContext db) =>
        {
            var slots = await db.PickemBracketSlots
                .AsNoTracking()
                .OrderBy(s => s.SlotIndex)
                .ToListAsync();

            var response = new
            {
                QuarterFinals = slots.Select(s => new
                {
                    s.SlotIndex,
                    s.TeamName,
                    s.TeamFlag,
                    s.IsEliminated,
                    s.EliminatedBy
                }).ToList(),
                Deadline = Deadline
            };

            return Results.Ok(response);
        });

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/pickem/status
        // ─────────────────────────────────────────────────────────────────────
        group.MapGet("/status", async (AppDbContext db) =>
        {
            var participants = await db.Participants
                .AsNoTracking()
                .ToListAsync();

            var entryParticipantIds = await db.PickemEntries
                .AsNoTracking()
                .Select(e => e.ParticipantId)
                .ToListAsync();

            var completed = participants
                .Where(p => entryParticipantIds.Contains(p.Id))
                .Select(p => new { ParticipantId = p.Id, ParticipantName = p.Name })
                .ToList();

            var pending = participants
                .Where(p => !entryParticipantIds.Contains(p.Id))
                .Select(p => new { ParticipantId = p.Id, ParticipantName = p.Name })
                .ToList();

            var isRevealed = completed.Count >= TotalParticipants
                          || DateTimeOffset.UtcNow >= Deadline;

            return Results.Ok(new { Completed = completed, Pending = pending, IsRevealed = isRevealed });
        });

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/pickem/standings
        // ─────────────────────────────────────────────────────────────────────
        group.MapGet("/standings", async (AppDbContext db) =>
        {
            var standings = await db.PickemStandings
                .AsNoTracking()
                .Include(s => s.Participant)
                .OrderByDescending(s => s.TotalPickemPoints)
                .ToListAsync();

            // Include participants with no standing yet (0 pts)
            var participantIds = standings.Select(s => s.ParticipantId).ToHashSet();
            var allParticipants = await db.Participants.AsNoTracking().ToListAsync();

            var result = standings.Select(s =>
            {
                var correctPicks = db.PickemPicks
                    .Where(p => p.Entry.ParticipantId == s.ParticipantId && p.IsCorrect == true)
                    .Count();
                return new
                {
                    s.ParticipantId,
                    ParticipantName = s.Participant.Name,
                    s.TotalPickemPoints,
                    CorrectPicks = correctPicks
                };
            }).ToList<object>();

            // Participants that have a bracket but no standing row yet
            var zeroes = allParticipants
                .Where(p => !participantIds.Contains(p.Id))
                .Select(p => (object)new
                {
                    ParticipantId = p.Id,
                    ParticipantName = p.Name,
                    TotalPickemPoints = 0,
                    CorrectPicks = 0
                });

            return Results.Ok(result.Concat(zeroes).ToList());
        });

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/pickem/entry/{participantId}
        // ─────────────────────────────────────────────────────────────────────
        group.MapGet("/entry/{participantId:int}", async (int participantId, HttpContext httpContext, AppDbContext db) =>
        {
            // Parse requesting participant from header
            int? requesterId = null;
            if (httpContext.Request.Headers.TryGetValue("X-Participant-Id", out var val)
                && int.TryParse(val, out var rid))
                requesterId = rid;

            var isRevealed = (await db.PickemEntries.CountAsync()) >= TotalParticipants
                          || DateTimeOffset.UtcNow >= Deadline;

            // Only the owner can view their own bracket if not yet revealed
            if (!isRevealed && requesterId != participantId)
                return Results.StatusCode(403);

            var entry = await db.PickemEntries
                .AsNoTracking()
                .Include(e => e.Picks)
                .Include(e => e.Participant)
                .FirstOrDefaultAsync(e => e.ParticipantId == participantId);

            if (entry is null)
                return Results.NotFound();

            var response = new
            {
                entry.Id,
                entry.ParticipantId,
                ParticipantName = entry.Participant.Name,
                entry.CreatedAt,
                entry.IsLocked,
                Picks = entry.Picks
                    .OrderBy(p => p.Round)
                    .ThenBy(p => p.SlotIndex)
                    .Select(p => new
                    {
                        p.Round,
                        p.SlotIndex,
                        p.ChosenTeam,
                        p.ChosenTeamFlag,
                        p.IsCorrect,
                        p.PointsEarned
                    }).ToList()
            };

            return Results.Ok(response);
        });
        // ─────────────────────────────────────────────────────────────────────
        // POST /api/pickem/entry
        // ─────────────────────────────────────────────────────────────────────
        group.MapPost("/entry", async (PickemEntryRequest request, AppDbContext db) =>
        {
            // Deadline check
            if (DateTimeOffset.UtcNow >= Deadline)
                return Results.UnprocessableEntity("O prazo para envio do pick'em já encerrou.");

            // Duplicate check
            var existing = await db.PickemEntries
                .AnyAsync(e => e.ParticipantId == request.ParticipantId);
            if (existing)
                return Results.Conflict("Você já enviou seu pick'em.");

            // Picks count check
            if (request.Picks.Count != 7)
                return Results.UnprocessableEntity(
                    "São necessários exatamente 7 picks: 4 QF + 2 SF + 1 Final.");

            var qfPicks = request.Picks.Where(p => p.Round == "QUARTER_FINAL").ToList();
            var sfPicks = request.Picks.Where(p => p.Round == "SEMI_FINAL").ToList();
            var finalPicks = request.Picks.Where(p => p.Round == "FINAL").ToList();

            if (qfPicks.Count != 4 || sfPicks.Count != 2 || finalPicks.Count != 1)
                return Results.UnprocessableEntity(
                    "Picks inválidos: esperados 4 QF, 2 SF e 1 Final.");

            // Consistency: SF teams must have been picked in QF
            var qfTeams = qfPicks.Select(p => p.ChosenTeam).ToHashSet();
            foreach (var sf in sfPicks)
            {
                if (!qfTeams.Contains(sf.ChosenTeam))
                    return Results.UnprocessableEntity(
                        $"Time '{sf.ChosenTeam}' na semi não foi escolhido nas quartas.");
            }

            // Consistency: Final team must have been picked in SF
            var sfTeams = sfPicks.Select(p => p.ChosenTeam).ToHashSet();
            var champion = finalPicks[0].ChosenTeam;
            if (!sfTeams.Contains(champion))
                return Results.UnprocessableEntity(
                    $"Campeão '{champion}' não foi escolhido nas semifinais.");

            var entry = new PickemEntry
            {
                ParticipantId = request.ParticipantId,
                CreatedAt = DateTimeOffset.UtcNow,
                IsLocked = false,
                Picks = request.Picks.Select(p => new PickemPick
                {
                    Round = p.Round,
                    SlotIndex = p.SlotIndex,
                    ChosenTeam = p.ChosenTeam,
                    ChosenTeamFlag = p.ChosenTeamFlag
                }).ToList()
            };

            db.PickemEntries.Add(entry);

            // Create standing row if not exists
            var standingExists = await db.PickemStandings
                .AnyAsync(s => s.ParticipantId == request.ParticipantId);
            if (!standingExists)
            {
                db.PickemStandings.Add(new PickemStanding
                {
                    ParticipantId = request.ParticipantId,
                    TotalPickemPoints = 0
                });
            }

            await db.SaveChangesAsync();
            return Results.Created($"/api/pickem/entry/{request.ParticipantId}", new { entry.Id });
        });
    }
}

// ─── Request DTOs ─────────────────────────────────────────────────────────────
public record PickemEntryRequest(
    int ParticipantId,
    List<PickemPickRequest> Picks
);

public record PickemPickRequest(
    string Round,
    int SlotIndex,
    string ChosenTeam,
    string ChosenTeamFlag
);
