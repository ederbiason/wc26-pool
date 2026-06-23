using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.Models;

namespace WC26Pool.API.Services;

public class PredictionOrderService(AppDbContext db)
{
    public async Task GenerateOrderForDateAsync(DateOnly date)
    {
        var existingOrders = await db.DayPredictionOrders
            .Where(d => d.Date == date)
            .ToListAsync();

        if (existingOrders.Count != 0)
            return;

        var participants = await db.Participants
            .OrderByDescending(p => p.TotalPoints)
            .ThenBy(p => p.Name)
            .ToListAsync();

        var orders = participants.Select((p, index) => new DayPredictionOrder
        {
            Date = date,
            ParticipantId = p.Id,
            Order = index + 1,
            HasSubmittedAll = false
        }).ToList();

        db.DayPredictionOrders.AddRange(orders);
        await db.SaveChangesAsync();
    }

    public async Task<bool> CanParticipantPredictAsync(int participantId, DateOnly date, DateTimeOffset firstMatchTime)
    {
        var order = await db.DayPredictionOrders
            .FirstOrDefaultAsync(d => d.Date == date && d.ParticipantId == participantId);

        if (order is null)
            return false;

        if (order.Order == 1)
            return DateTimeOffset.UtcNow >= firstMatchTime.AddMinutes(-60);

        if (order.Order == 2)
        {
            var firstOrder = await db.DayPredictionOrders
                .FirstOrDefaultAsync(d => d.Date == date && d.Order == 1);
            return firstOrder?.HasSubmittedAll == true;
        }

        var secondOrder = await db.DayPredictionOrders
            .FirstOrDefaultAsync(d => d.Date == date && d.Order == 2);
        return secondOrder?.HasSubmittedAll == true;
    }

    public async Task UpdateSubmissionStatusAsync(int participantId, DateOnly date)
    {
        var order = await db.DayPredictionOrders
            .FirstOrDefaultAsync(d => d.Date == date && d.ParticipantId == participantId);

        if (order is null)
            return;

        var todayMatches = await db.Matches
            .Where(m => DateOnly.FromDateTime(m.MatchDate.Date) == date && m.Status == MatchStatus.NotStarted)
            .Select(m => m.Id)
            .ToListAsync();

        var participantPredictions = await db.Predictions
            .Where(p => p.ParticipantId == participantId && todayMatches.Contains(p.MatchId))
            .CountAsync();

        order.HasSubmittedAll = participantPredictions >= todayMatches.Count;
        await db.SaveChangesAsync();
    }
}
