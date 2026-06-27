using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.DTOs;
using WC26Pool.API.Models;

namespace WC26Pool.API.Services;

public class PredictionVisibilityService(AppDbContext db)
{
    private const int TotalParticipants = 6;

    public async Task<Dictionary<int, PredictionVisibilityDto>> GetVisibilityForMatchesAsync(
        Dictionary<int, MatchStatus> matchStatuses,
        int? requestingParticipantId)
    {
        var matchIds = matchStatuses.Keys;

        var allPredictions = await db.Predictions
            .AsNoTracking()
            .Include(p => p.Participant)
            .Where(p => matchIds.Contains(p.MatchId))
            .ToListAsync();

        var allParticipants = await db.Participants
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();

        var predictionsByMatch = allPredictions.GroupBy(p => p.MatchId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new Dictionary<int, PredictionVisibilityDto>();

        foreach (var (matchId, status) in matchStatuses)
        {
            var predictions = predictionsByMatch.GetValueOrDefault(matchId, []);
            var participantsWhoVoted = predictions.Select(p => p.ParticipantId).ToHashSet();

            var completed = allParticipants
                .Where(p => participantsWhoVoted.Contains(p.Id))
                .Select(p => new ParticipantSummaryDto(p.Id, p.Name))
                .ToList();

            var pending = allParticipants
                .Where(p => !participantsWhoVoted.Contains(p.Id))
                .Select(p => new ParticipantSummaryDto(p.Id, p.Name))
                .ToList();

            var isRevealed = IsRevealed(status, predictions.Count);

            List<PredictionDto> visiblePredictions;

            if (isRevealed)
            {
                visiblePredictions = predictions
                    .Select(p => PredictionDto.FromPrediction(p))
                    .ToList();
            }
            else if (requestingParticipantId.HasValue)
            {
                visiblePredictions = predictions
                    .Where(p => p.ParticipantId == requestingParticipantId.Value)
                    .Select(p => PredictionDto.FromPrediction(p))
                    .ToList();
            }
            else
            {
                visiblePredictions = [];
            }

            result[matchId] = new PredictionVisibilityDto(isRevealed, visiblePredictions, completed, pending);
        }

        return result;
    }

    public async Task<PredictionVisibilityDto> GetVisibilityForMatchAsync(
        int matchId,
        MatchStatus matchStatus,
        int? requestingParticipantId)
    {
        var allParticipants = await db.Participants
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();

        var predictions = await db.Predictions
            .AsNoTracking()
            .Include(p => p.Participant)
            .Where(p => p.MatchId == matchId)
            .ToListAsync();

        var participantsWhoVoted = predictions.Select(p => p.ParticipantId).ToHashSet();

        var completed = allParticipants
            .Where(p => participantsWhoVoted.Contains(p.Id))
            .Select(p => new ParticipantSummaryDto(p.Id, p.Name))
            .ToList();

        var pending = allParticipants
            .Where(p => !participantsWhoVoted.Contains(p.Id))
            .Select(p => new ParticipantSummaryDto(p.Id, p.Name))
            .ToList();

        var isRevealed = IsRevealed(matchStatus, predictions.Count);

        List<PredictionDto> visiblePredictions;

        if (isRevealed)
        {
            visiblePredictions = predictions
                .Select(p => PredictionDto.FromPrediction(p))
                .ToList();
        }
        else if (requestingParticipantId.HasValue)
        {
            visiblePredictions = predictions
                .Where(p => p.ParticipantId == requestingParticipantId.Value)
                .Select(p => PredictionDto.FromPrediction(p))
                .ToList();
        }
        else
        {
            visiblePredictions = [];
        }

        return new PredictionVisibilityDto(isRevealed, visiblePredictions, completed, pending);
    }

    private static bool IsRevealed(MatchStatus status, int predictionCount) =>
        predictionCount >= TotalParticipants ||
        status == MatchStatus.InProgress ||
        status == MatchStatus.Finished;
}
