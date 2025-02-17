using DogTracker.Models;

namespace DogTracker.Interfaces;

public interface IDogService
{
    Task AddDogAsync(Dog dog);
}