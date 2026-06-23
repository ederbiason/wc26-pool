using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.Models;

namespace WC26Pool.API.Services;

public class ScoringService(AppDbContext db)
{
    public async Task CalculatePointsForMatchAsync(int matchId)
    {
        var match = await db.Matches.FindAsync(matchId);

        if (match is null || match.Status != MatchStatus.Finished || match.PointsCalculated)
            return;

        if (match.HomeScore is null || match.AwayScore is null)
            return;

        var predictions = await db.Predictions
            .Include(p => p.Participant)
            .Where(p => p.MatchId == matchId)
            .ToListAsync();

        foreach (var prediction in predictions)
        {
            prediction.PointsEarned = CalculatePoints(
                prediction.PredictedHomeScore,
                prediction.PredictedAwayScore,
                match.HomeScore.Value,
                match.AwayScore.Value
            );

            prediction.Participant.TotalPoints += prediction.PointsEarned.Value;
        }

        match.PointsCalculated = true;
        await db.SaveChangesAsync();
    }

    private static int CalculatePoints(int predHome, int predAway, int actualHome, int actualAway)
    {
        if (predHome == actualHome && predAway == actualAway)
            return 2;

        var predictedResult = Math.Sign(predHome - predAway);
        var actualResult = Math.Sign(actualHome - actualAway);

        if (predictedResult == actualResult)
            return 1;

        return 0;
    }
}
