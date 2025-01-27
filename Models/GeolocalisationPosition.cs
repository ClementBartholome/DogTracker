namespace DogTracker.Models;

public class GeolocationPosition
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Accuracy { get; set; }
    public DateTime Timestamp { get; set; }
}