namespace WC26Pool.API.Models;

public class PickemStanding
{
    public int Id { get; set; }
    public int ParticipantId { get; set; }
    public Participant Participant { get; set; } = null!;
    public int TotalPickemPoints { get; set; }
}
