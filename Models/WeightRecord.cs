namespace DogTracker.Models;

public class WeightRecord
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public double Weight { get; set; }
    public int DogId { get; set; }
}