namespace WC26Pool.API.Models;

public class Participant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public int TotalPoints { get; set; }

    public ICollection<Prediction> Predictions { get; set; } = [];
}
