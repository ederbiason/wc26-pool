using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.Models;

namespace WC26Pool.API.Services;

public class PredictionVisibilityService(AppDbContext db)
{
    private const int TotalParticipants = 6;

    public async Task<bool> ArePredictionsRevealedAsync(DateOnly date)
    {
        var visibility = await db.DayVisibilities
            .FirstOrDefaultAsync(v => v.Date == date);

        if (visibility is not null && (visibility.IsRevealed || visibility.ForceRevealedByAdmin))
            return true;

        var todayMatches = await db.Matches
            .Where(m => DateOnly.FromDateTime(m.MatchDate.Date) == date)
            .Select(m => m.Id)
            .ToListAsync();

        if (todayMatches.Count == 0)
            return false;

        var predictionsPerParticipant = await db.Predictions
            .Where(p => todayMatches.Contains(p.MatchId))
            .GroupBy(p => p.ParticipantId)
            .Select(g => new { ParticipantId = g.Key, Count = g.Count() })
            .ToListAsync();

        var allPredicted = predictionsPerParticipant.Count == TotalParticipants &&
                           predictionsPerParticipant.All(p => p.Count >= todayMatches.Count);

        if (allPredicted)
        {
            await EnsureVisibilityRecordAsync(date, true);
        }

        return allPredicted;
    }

    public async Task ForceRevealAsync(DateOnly date)
    {
        var visibility = await db.DayVisibilities
            .FirstOrDefaultAsync(v => v.Date == date);

        if (visibility is null)
        {
            db.DayVisibilities.Add(new DayVisibility
            {
                Date = date,
                IsRevealed = false,
                ForceRevealedByAdmin = true
            });
        }
        else
        {
            visibility.ForceRevealedByAdmin = true;
        }

        await db.SaveChangesAsync();
    }

    private async Task EnsureVisibilityRecordAsync(DateOnly date, bool revealed)
    {
        var visibility = await db.DayVisibilities
            .FirstOrDefaultAsync(v => v.Date == date);

        if (visibility is null)
        {
            db.DayVisibilities.Add(new DayVisibility
            {
                Date = date,
                IsRevealed = revealed,
                ForceRevealedByAdmin = false
            });
            await db.SaveChangesAsync();
        }
        else if (!visibility.IsRevealed)
        {
            visibility.IsRevealed = revealed;
            await db.SaveChangesAsync();
        }
    }
}
