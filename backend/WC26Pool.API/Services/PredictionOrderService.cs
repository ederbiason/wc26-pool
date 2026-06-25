using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.Models;

namespace WC26Pool.API.Services;

public class PredictionOrderService(AppDbContext db)
{
    private static readonly TimeZoneInfo BrasiliaZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    // Converts a match's UtcDateTime to the Brasília calendar day
    public static DateOnly GetBrasiliaDate(DateTimeOffset matchDate)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(matchDate.UtcDateTime, BrasiliaZone);
        return DateOnly.FromDateTime(local.Date);
    }

    /// <summary>
    /// Generates the prediction order for a given Brasília day.
    /// If the order already exists it is skipped (idempotent).
    /// Pass forceRegenerate = true to delete and recreate (used on startup to fix stale orders).
    /// </summary>
    public async Task GenerateOrderForDateAsync(DateOnly date, bool forceRegenerate = false)
    {
        var existingOrders = await db.DayPredictionOrders
            .Where(d => d.Date == date)
            .ToListAsync();

        if (existingOrders.Count != 0)
        {
            if (!forceRegenerate) return;
            db.DayPredictionOrders.RemoveRange(existingOrders);
            await db.SaveChangesAsync();
        }

        var participants = await db.Participants
            .OrderByDescending(p => p.TotalPoints)
            .ThenBy(p => p.Name)
            .ToListAsync();

        // Tie-breaking rule:
        // – If 1st and 2nd are tied → everyone gets Order = 3 (free-for-all)
        // – If 1st and 2nd differ → 1st gets Order 1, 2nd gets Order 2, rest get Order 3
        // – Edge case: single participant → Order 1

        List<DayPredictionOrder> orders;

        if (participants.Count >= 2 && participants[0].TotalPoints == participants[1].TotalPoints)
        {
            // Tied at the top: everyone is free
            orders = participants.Select(p => new DayPredictionOrder
            {
                Date = date,
                ParticipantId = p.Id,
                Order = 3,
                HasSubmittedAll = false
            }).ToList();
        }
        else
        {
            orders = participants.Select((p, index) => new DayPredictionOrder
            {
                Date = date,
                ParticipantId = p.Id,
                Order = Math.Min(index + 1, 3), // cap at 3; 1st=1, 2nd=2, rest=3
                HasSubmittedAll = false
            }).ToList();
        }

        db.DayPredictionOrders.AddRange(orders);
        await db.SaveChangesAsync();
    }

    public async Task<bool> CanParticipantPredictAsync(int participantId, DateOnly date, DateTimeOffset firstMatchTime)
    {
        var order = await db.DayPredictionOrders
            .FirstOrDefaultAsync(d => d.Date == date && d.ParticipantId == participantId);

        if (order is null)
            return false;

        // Order 3 = always free (tied scenario or regular 3rd+)
        if (order.Order >= 3)
            return true;

        if (order.Order == 1)
            return DateTimeOffset.UtcNow >= firstMatchTime.AddMinutes(-60);

        // Order 2: wait for Order 1 participant to have submitted all
        if (order.Order == 2)
        {
            var firstOrder = await db.DayPredictionOrders
                .FirstOrDefaultAsync(d => d.Date == date && d.Order == 1);
            return firstOrder?.HasSubmittedAll == true;
        }

        return false;
    }

    public async Task UpdateSubmissionStatusAsync(int participantId, DateOnly date)
    {
        var order = await db.DayPredictionOrders
            .FirstOrDefaultAsync(d => d.Date == date && d.ParticipantId == participantId);

        if (order is null)
            return;

        // Count NotStarted matches for this Brasília day using UTC boundaries
        var startLocal = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, BrasiliaZone);
        var endUtc   = startUtc.AddDays(1);

        var todayMatchIds = await db.Matches
            .Where(m => m.MatchDate.UtcDateTime >= startUtc &&
                        m.MatchDate.UtcDateTime <  endUtc &&
                        m.Status == MatchStatus.NotStarted)
            .Select(m => m.Id)
            .ToListAsync();

        var participantPredictions = await db.Predictions
            .Where(p => p.ParticipantId == participantId && todayMatchIds.Contains(p.MatchId))
            .CountAsync();

        order.HasSubmittedAll = participantPredictions >= todayMatchIds.Count;
        await db.SaveChangesAsync();
    }
}
