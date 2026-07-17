using WC26Pool.API.Services;

namespace WC26Pool.API.Endpoints;

public static class StatsEndpoints
{
    public static void MapStatsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/stats", async (StatsService statsService) =>
        {
            var stats = await statsService.GetStatsAsync();

            return Results.Ok(new
            {
                participants = stats.Participants.Select(p => new
                {
                    participantId = p.ParticipantId,
                    participantName = p.ParticipantName,
                    totalPoints = p.TotalPoints,
                    totalPredictions = p.TotalPredictions,
                    exactScores = p.ExactScores,
                    correctResults = p.CorrectResults,
                    wrongPredictions = p.WrongPredictions,
                    hitRate = p.HitRate,
                    pointsByStage = new
                    {
                        groupStage = p.GroupStagePoints,
                        knockoutStage = p.KnockoutStagePoints
                    },
                    pickemPoints = p.PickemPoints
                }),
                highlights = new
                {
                    mostExactScores = new
                    {
                        participantName = stats.Highlights.MostExactScores.ParticipantName,
                        count = (int)stats.Highlights.MostExactScores.Value
                    },
                    bestHitRate = new
                    {
                        participantName = stats.Highlights.BestHitRate.ParticipantName,
                        rate = stats.Highlights.BestHitRate.Value
                    },
                    bestKnockout = new
                    {
                        participantName = stats.Highlights.BestKnockout.ParticipantName,
                        points = (int)stats.Highlights.BestKnockout.Value
                    },
                    pickemLeader = new
                    {
                        participantName = stats.Highlights.PickemLeader.ParticipantName,
                        points = (int)stats.Highlights.PickemLeader.Value
                    }
                },
                matchHighlights = stats.MatchHighlights.Select(m => new
                {
                    matchId = m.MatchId,
                    homeTeam = m.HomeTeam,
                    awayTeam = m.AwayTeam,
                    homeScore = m.HomeScore,
                    awayScore = m.AwayScore,
                    stage = m.Stage,
                    exactScoreCount = m.ExactScoreCount,
                    correctResultCount = m.CorrectResultCount
                })
            });
        });
    }
}
