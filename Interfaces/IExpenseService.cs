using DogTracker.Models;

namespace DogTracker.Interfaces;

public interface IExpenseService
{
    Task<List<Expense>> GetExpensesByMonth(int dogId, int year, int month);
    Task<List<Expense>> GetYearlyExpenses(int dogId, int year);
    Task<Expense> GetLastExpense(int dogId);
    Task DeleteExpenseAsync(int dogId, int expenseId);
    Task AddExpenseAsync(int dogId, Expense expense);
}