using DogTracker.Models;

namespace DogTracker.ViewModels;

public class WalkDataViewModel
{
    public GeolocationPosition[] Positions { get; set; } = [];
}