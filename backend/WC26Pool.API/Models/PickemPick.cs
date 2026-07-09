namespace WC26Pool.API.Models;

public class PickemPick
{
    public int Id { get; set; }
    public int PickemEntryId { get; set; }
    public PickemEntry Entry { get; set; } = null!;

    /// "QUARTER_FINAL", "SEMI_FINAL", "FINAL"
    public string Round { get; set; } = string.Empty;

    /// 0-3 for QF, 0-1 for SF, 0 for Final
    public int SlotIndex { get; set; }

    public string ChosenTeam { get; set; } = string.Empty;
    public string ChosenTeamFlag { get; set; } = string.Empty;

    public bool? IsCorrect { get; set; }
    public int? PointsEarned { get; set; }
}
