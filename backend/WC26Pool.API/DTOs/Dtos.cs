using WC26Pool.API.Models;

namespace WC26Pool.API.DTOs;

public record MatchDto(
    int Id,
    string ExternalId,
    string HomeTeam,
    string AwayTeam,
    string HomeTeamFlag,
    string AwayTeamFlag,
    DateTimeOffset MatchDate,
    string Status,
    int? HomeScore,
    int? AwayScore,
    bool PointsCalculated
)
{
    public static MatchDto FromMatch(Match m) => new(
        m.Id,
        m.ExternalId,
        m.HomeTeam,
        m.AwayTeam,
        m.HomeTeamFlag,
        m.AwayTeamFlag,
        m.MatchDate,
        m.Status.ToString(),
        m.HomeScore,
        m.AwayScore,
        m.PointsCalculated
    );
}

public record CreatePredictionRequest(
    int ParticipantId,
    int MatchId,
    int PredictedHomeScore,
    int PredictedAwayScore
);

public record PredictionDto(
    int Id,
    int ParticipantId,
    string ParticipantName,
    int MatchId,
    int PredictedHomeScore,
    int PredictedAwayScore,
    DateTimeOffset CreatedAt,
    int? PointsEarned
)
{
    public static PredictionDto FromPrediction(Prediction p) => new(
        p.Id,
        p.ParticipantId,
        p.Participant?.Name ?? string.Empty,
        p.MatchId,
        p.PredictedHomeScore,
        p.PredictedAwayScore,
        p.CreatedAt,
        p.PointsEarned
    );
}

public record DayPredictionOrderDto(
    int ParticipantId,
    string ParticipantName,
    int Order,
    bool HasSubmittedAll
);

public record ParticipantDto(
    int Id,
    string Name,
    bool IsAdmin,
    int TotalPoints
)
{
    public static ParticipantDto FromParticipant(Participant p) => new(
        p.Id,
        p.Name,
        p.IsAdmin,
        p.TotalPoints
    );
}

public record RankingEntryDto(
    int Position,
    int ParticipantId,
    string Name,
    int TotalPoints
);
