using DogTracker.Models;

namespace DogTracker.Interfaces;

public interface IWeightService
{
    Task<List<WeightRecord>> GetWeightRecordsAsync(int dogId);
    Task<WeightRecord> GetLastWeightRecord(int dogId);
    Task AddWeightRecordAsync(int dogId, WeightRecord weight);
    Task DeleteWeightRecordAsync(int dogId, int weightId);
}