using DogTracker.Models;

namespace DogTracker.ViewModels;

public class WalkDataViewModel
{
    public DateTime StartTime { get; set; }
    public GeolocationPosition[] Positions { get; set; } = [];
}