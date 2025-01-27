using DogTracker.Models;

namespace DogTracker.Interfaces;

public interface IDogService
{
    Task<List<Walk>> GetRecentWalksAsync(int dogId);
    Task AddWalkAsync(int dogId, Walk? walk);
    Task<List<WeightRecord>> GetWeightHistoryAsync(int dogId);
    Task AddWeightRecordAsync(int dogId, WeightRecord weight);
    Task<List<Expense>> GetExpensesAsync(int dogId, DateTime startDate, DateTime endDate);
    Task AddExpenseAsync(int dogId, Expense expense);
    Task AddDogAsync(Dog dog);
}