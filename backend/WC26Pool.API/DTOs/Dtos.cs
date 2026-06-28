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
    bool PointsCalculated,
    string Stage,
    string? GroupName,
    string Duration,
    int? RegularTimeHomeScore,
    int? RegularTimeAwayScore,
    int? PenaltyHomeScore,
    int? PenaltyAwayScore
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
        m.PointsCalculated,
        m.Stage,
        m.GroupName,
        m.Duration,
        m.RegularTimeHomeScore,
        m.RegularTimeAwayScore,
        m.PenaltyHomeScore,
        m.PenaltyAwayScore
    );
}

public record MatchWithVisibilityDto(
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
    bool PointsCalculated,
    string Stage,
    string? GroupName,
    string Duration,
    int? RegularTimeHomeScore,
    int? RegularTimeAwayScore,
    int? PenaltyHomeScore,
    int? PenaltyAwayScore,
    PredictionVisibilityDto PredictionVisibility
)
{
    public static MatchWithVisibilityDto FromMatch(Match m, PredictionVisibilityDto visibility) => new(
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
        m.PointsCalculated,
        m.Stage,
        m.GroupName,
        m.Duration,
        m.RegularTimeHomeScore,
        m.RegularTimeAwayScore,
        m.PenaltyHomeScore,
        m.PenaltyAwayScore,
        visibility
    );
}

public record PredictionVisibilityDto(
    bool IsRevealed,
    IReadOnlyList<PredictionDto> Predictions,
    IReadOnlyList<ParticipantSummaryDto> CompletedParticipants,
    IReadOnlyList<ParticipantSummaryDto> PendingParticipants
);

public record ParticipantSummaryDto(int ParticipantId, string ParticipantName);

public record CreatePredictionRequest(
    int ParticipantId,
    int MatchId,
    int PredictedHomeScore,
    int PredictedAwayScore,
    string? PenaltyWinnerTeam
);

public record PredictionDto(
    int Id,
    int ParticipantId,
    string ParticipantName,
    int MatchId,
    int PredictedHomeScore,
    int PredictedAwayScore,
    string? PenaltyWinnerTeam,
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
        p.PenaltyWinnerTeam,
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

public record UpcomingDayDto(string Date, IReadOnlyList<MatchDto> Matches);
