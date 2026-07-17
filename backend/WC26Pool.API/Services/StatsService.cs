using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;

namespace WC26Pool.API.Services;

public record ParticipantStats(
    int ParticipantId,
    string ParticipantName,
    int TotalPoints,
    int TotalPredictions,
    int ExactScores,
    int CorrectResults,
    int WrongPredictions,
    float HitRate,
    int GroupStagePoints,
    int KnockoutStagePoints,
    int PickemPoints
);

public record HighlightEntry(string ParticipantName, float Value);

public record GlobalHighlights(
    HighlightEntry MostExactScores,
    HighlightEntry BestHitRate,
    HighlightEntry BestKnockout,
    HighlightEntry PickemLeader
);

public record MatchHighlight(
    int MatchId,
    string HomeTeam,
    string AwayTeam,
    int? HomeScore,
    int? AwayScore,
    string Stage,
    int ExactScoreCount,
    int CorrectResultCount
);

public record StatsResponse(
    IReadOnlyList<ParticipantStats> Participants,
    GlobalHighlights Highlights,
    IReadOnlyList<MatchHighlight> MatchHighlights
);

public class StatsService(AppDbContext db)
{
    public async Task<StatsResponse> GetStatsAsync()
    {
        // 1. All scored predictions with their match and participant
        var predictions = await db.Predictions
            .AsNoTracking()
            .Include(p => p.Match)
            .Include(p => p.Participant)
            .Where(p => p.PointsEarned.HasValue)
            .ToListAsync();

        // 2. Participants with their TotalPoints
        var participants = await db.Participants
            .AsNoTracking()
            .ToListAsync();

        // 3. PickemStandings
        var pickemStandings = await db.PickemStandings
            .AsNoTracking()
            .ToDictionaryAsync(s => s.ParticipantId, s => s.TotalPickemPoints);

        // 4. Group predictions by participant in memory
        var predsByParticipant = predictions
            .GroupBy(p => p.ParticipantId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var knockoutStages = new HashSet<string>
            { "LAST_32", "LAST_16", "QUARTER_FINALS", "SEMI_FINALS", "THIRD_PLACE", "FINAL" };

        var participantStats = participants.Select(part =>
        {
            var preds = predsByParticipant.GetValueOrDefault(part.Id, []);
            var total = preds.Count;
            var exact = preds.Count(p => p.PointsEarned == 2);
            var correct = preds.Count(p => p.PointsEarned == 1);
            var wrong = preds.Count(p => p.PointsEarned == 0);
            var hitRate = total > 0 ? (float)(exact + correct) / total * 100f : 0f;
            var groupPts = preds.Where(p => p.Match.Stage == "GROUP_STAGE").Sum(p => p.PointsEarned ?? 0);
            var knockoutPts = preds.Where(p => knockoutStages.Contains(p.Match.Stage)).Sum(p => p.PointsEarned ?? 0);
            var pickemPts = pickemStandings.GetValueOrDefault(part.Id, 0);

            return new ParticipantStats(
                part.Id,
                part.Name,
                part.TotalPoints,
                total,
                exact,
                correct,
                wrong,
                MathF.Round(hitRate, 1),
                groupPts,
                knockoutPts,
                pickemPts
            );
        }).ToList();

        // 5. Global highlights (only participants who made at least 1 prediction)
        var active = participantStats.Where(p => p.TotalPredictions > 0).ToList();

        var highlights = BuildHighlights(active, participantStats);

        // 6. Match highlights — group predictions by match
        var matchHighlights = predictions
            .GroupBy(p => p.MatchId)
            .Where(g => g.First().Match.PointsCalculated)
            .Select(g =>
            {
                var match = g.First().Match;
                return new MatchHighlight(
                    match.Id,
                    match.HomeTeam,
                    match.AwayTeam,
                    match.HomeScore,
                    match.AwayScore,
                    match.Stage,
                    g.Count(p => p.PointsEarned == 2),
                    g.Count(p => p.PointsEarned == 1)
                );
            })
            .OrderByDescending(m => m.ExactScoreCount)
            .ThenByDescending(m => m.CorrectResultCount)
            .ToList();

        return new StatsResponse(participantStats, highlights, matchHighlights);
    }

    private static GlobalHighlights BuildHighlights(
        List<ParticipantStats> active,
        List<ParticipantStats> all)
    {
        var fallback = new HighlightEntry("—", 0);

        var mostExact = active.Count > 0
            ? active.MaxBy(p => p.ExactScores)!
            : null;

        var bestHit = active.Count > 0
            ? active.MaxBy(p => p.HitRate)!
            : null;

        var bestKnockout = all.Count > 0
            ? all.MaxBy(p => p.KnockoutStagePoints)!
            : null;

        var pickemLeader = all.Count > 0
            ? all.MaxBy(p => p.PickemPoints)!
            : null;

        return new GlobalHighlights(
            mostExact is not null
                ? new HighlightEntry(mostExact.ParticipantName, mostExact.ExactScores)
                : fallback,
            bestHit is not null
                ? new HighlightEntry(bestHit.ParticipantName, bestHit.HitRate)
                : fallback,
            bestKnockout is not null
                ? new HighlightEntry(bestKnockout.ParticipantName, bestKnockout.KnockoutStagePoints)
                : fallback,
            pickemLeader is not null
                ? new HighlightEntry(pickemLeader.ParticipantName, pickemLeader.PickemPoints)
                : fallback
        );
    }
}
