using DogTracker.Models;

namespace DogTracker.Interfaces;

public interface ILocationService
{
    Task<GeolocationPosition> GetCurrentPositionAsync();
    event EventHandler<GeolocationPosition> OnPositionChanged;
    Task StartWatchingPositionAsync();
    Task StopWatchingPositionAsync();
    Task SyncPositions(GeolocationPosition[] newPositions);
}