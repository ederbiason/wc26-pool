namespace WC26Pool.API.Models;

public class PickemEntry
{
    public int Id { get; set; }
    public int ParticipantId { get; set; }
    public Participant Participant { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsLocked { get; set; }
    public List<PickemPick> Picks { get; set; } = [];
}
