namespace WC26Pool.API.Models;

public class DayPredictionOrder
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public int ParticipantId { get; set; }
    public int Order { get; set; }
    public bool HasSubmittedAll { get; set; }

    public Participant Participant { get; set; } = null!;
}
