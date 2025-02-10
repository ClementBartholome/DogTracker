using DogTracker.Models;

namespace DogTracker.Interfaces;

public interface IDogService
{
    Task<List<WeightRecord>> GetWeightHistoryAsync(int dogId);
    Task AddWeightRecordAsync(int dogId, WeightRecord weight);
    Task AddDogAsync(Dog dog);
}