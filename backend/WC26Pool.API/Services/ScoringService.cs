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
                prediction.PenaltyWinnerTeam,
                match.HomeScore.Value,
                match.AwayScore.Value,
                match.Stage,
                match.Duration,
                match.PenaltyHomeScore,
                match.PenaltyAwayScore
            );

            prediction.Participant.TotalPoints += prediction.PointsEarned.Value;
        }

        match.PointsCalculated = true;
        await db.SaveChangesAsync();
    }

    private static int CalculatePoints(
        int predHome, int predAway, string? predPenaltyWinner,
        int actualHome, int actualAway, string stage, string duration,
        int? penaltyHome, int? penaltyAway)
    {
        if (stage == "GROUP_STAGE")
        {
            if (predHome == actualHome && predAway == actualAway)
                return 2;

            var predictedResult = Math.Sign(predHome - predAway);
            var actualResult = Math.Sign(actualHome - actualAway);

            if (predictedResult == actualResult)
                return 1;

            return 0;
        }

        // Knockout Stage
        var actualWinner = "DRAW";
        if (duration == "REGULAR" || duration == "EXTRA_TIME")
        {
            if (actualHome > actualAway) actualWinner = "HOME";
            else if (actualAway > actualHome) actualWinner = "AWAY";
        }
        else if (duration == "PENALTY_SHOOTOUT")
        {
            if (penaltyHome > penaltyAway) actualWinner = "HOME";
            else if (penaltyAway > penaltyHome) actualWinner = "AWAY";
        }

        // 3 pontos
        if (predHome == actualHome && predAway == actualAway && 
            duration == "PENALTY_SHOOTOUT" && predPenaltyWinner == actualWinner)
        {
            return 3;
        }

        // 2 pontos
        if (predHome == actualHome && predAway == actualAway)
        {
            return 2;
        }

        // 1 ponto
        var predictedWinner = "DRAW";
        if (predHome > predAway) predictedWinner = "HOME";
        else if (predAway > predHome) predictedWinner = "AWAY";
        else if (predPenaltyWinner != null) predictedWinner = predPenaltyWinner;

        if (predictedWinner == actualWinner && predictedWinner != "DRAW")
        {
            return 1;
        }

        return 0;
    }
}
