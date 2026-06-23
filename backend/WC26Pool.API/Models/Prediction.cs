namespace WC26Pool.API.Models;

public class Prediction
{
    public int Id { get; set; }
    public int ParticipantId { get; set; }
    public int MatchId { get; set; }
    public int PredictedHomeScore { get; set; }
    public int PredictedAwayScore { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int? PointsEarned { get; set; }

    public Participant Participant { get; set; } = null!;
    public Match Match { get; set; } = null!;
}
