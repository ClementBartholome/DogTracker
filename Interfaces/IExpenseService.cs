using DogTracker.Models;

namespace DogTracker.Interfaces;

public interface IExpenseService
{
    Task<List<Expense>> GetExpensesAsync(int dogId, int year, int month);
    Task<Expense> GetLastExpense(int dogId);
    Task DeleteExpenseAsync(int dogId, int expenseId);
    Task AddExpenseAsync(int dogId, Expense expense);
}