namespace DogTracker.Models;

public class Walk
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double Distance { get; set; } // en kilomètres
    public string? Route { get; set; }
    public string? Notes { get; set; }
    public int DogId { get; set; }
}