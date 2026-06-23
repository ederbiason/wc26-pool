namespace WC26Pool.API.Models;

public class DayVisibility
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public bool IsRevealed { get; set; }
    public bool ForceRevealedByAdmin { get; set; }
}
