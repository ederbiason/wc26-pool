namespace WC26Pool.API.Models;

public enum MatchStatus
{
    NotStarted,
    InProgress,
    Finished
}

public class Match
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public string HomeTeamFlag { get; set; } = string.Empty;
    public string AwayTeamFlag { get; set; } = string.Empty;
    public DateTimeOffset MatchDate { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.NotStarted;
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public bool PointsCalculated { get; set; }

    public string Stage { get; set; } = "GROUP_STAGE";
    public string? GroupName { get; set; }
    public string Duration { get; set; } = "REGULAR";
    public int? RegularTimeHomeScore { get; set; }
    public int? RegularTimeAwayScore { get; set; }
    public int? PenaltyHomeScore { get; set; }
    public int? PenaltyAwayScore { get; set; }

    public ICollection<Prediction> Predictions { get; set; } = [];
}
