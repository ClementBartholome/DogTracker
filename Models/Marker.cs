using DogTracker.Enums;

namespace DogTracker.Models;

public class Marker
{
    public int Id { get; set; }
    public TypeMarkerEnum Type { get; set; }
    public string Localisation { get; set; }
    public string Emplacement { get; set; }
    public GeoJson GeoJson { get; set; }
}