namespace WC26Pool.API.Models;

public class PickemBracketSlot
{
    public int Id { get; set; }
    public int SlotIndex { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string TeamFlag { get; set; } = string.Empty;
    public bool IsEliminated { get; set; }
    public string? EliminatedBy { get; set; }
}
