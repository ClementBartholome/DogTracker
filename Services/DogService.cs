using DogTracker.Interfaces;
using DogTracker.Models;
using DogTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace DogTracker.Services;

public class DogService : IDogService
{
    private readonly AppDbContext _context;

    public DogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Walk>> GetRecentWalksAsync(int dogId)
    {
        return await _context.Walks
            .Where(w => w.DogId == dogId)
            .OrderByDescending(w => w.StartTime)
            .ToListAsync();
    }

    public async Task AddWalkAsync(int dogId, Walk? walk)
    {
        walk.DogId = dogId;
        walk.StartTime = walk.StartTime.ToUniversalTime();
        walk.EndTime = walk.EndTime.ToUniversalTime();
        _context.Walks.Add(walk);
        await _context.SaveChangesAsync();
    }

    public async Task<List<WeightRecord>> GetWeightHistoryAsync(int dogId)
    {
        return await _context.WeightRecords
            .Where(w => w.DogId == dogId)
            .OrderByDescending(w => w.Date)
            .ToListAsync();
    }

    public async Task AddWeightRecordAsync(int dogId, WeightRecord weight)
    {
        weight.DogId = dogId;
        _context.WeightRecords.Add(weight);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Expense>> GetExpensesAsync(int dogId, DateTime startDate, DateTime endDate)
    {
        startDate = startDate.ToUniversalTime();
        endDate = endDate.ToUniversalTime();
        return await _context.Expenses
            .Where(e => e.DogId == dogId && e.Date >= startDate && e.Date <= endDate)
            .ToListAsync();
    }

    public async Task AddExpenseAsync(int dogId, Expense expense)
    {
        expense.DogId = dogId;
        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();
    }
    
    public async Task AddDogAsync(Dog dog)
    {
        _context.Dogs.Add(dog);
        await _context.SaveChangesAsync();
    }
    
    public async Task<Walk?> GetWalkByIdAsync(int walkId)
    {
        return await _context.Walks.FindAsync(walkId);
    }
}