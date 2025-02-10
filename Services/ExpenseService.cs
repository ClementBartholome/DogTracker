using DogTracker.Data;
using DogTracker.Interfaces;
using DogTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace DogTracker.Services;

public class ExpenseService(AppDbContext context, ILogger<DogService> logger) : IExpenseService
{
    public async Task<List<Expense>> GetExpensesAsync(int dogId, int year, int month)
    {
        try
        {
            var startDate = new DateTime(year, month, 1).ToUniversalTime();
            var endDate = startDate.AddMonths(1).AddSeconds(-1).ToUniversalTime().AddHours(1);
            
            logger.LogInformation("Récupération des dépenses pour le chien {DogId} du {StartDate} au {EndDate}", dogId, startDate, endDate);
            
            var expenses = await context.Expenses
                .Where(e => e.DogId == dogId && e.Date >= startDate && e.Date <= endDate)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
            
            logger.LogInformation("Dépenses récupérées avec succès pour le chien {DogId}", dogId);
            return expenses;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération des dépenses pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible de récupérer les dépenses.", ex);
        }
    }

    public async Task<Expense> GetLastExpense(int dogId)
    {
        try
        {
            logger.LogInformation("Récupération de la dernière dépense pour le chien {DogId}", dogId);
            var expense = await context.Expenses
                .Where(e => e.DogId == dogId)
                .OrderByDescending(e => e.Date)
                .FirstOrDefaultAsync();
            
            logger.LogInformation("Dernière dépense récupérée avec succès pour le chien {DogId}", dogId);
            return expense;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération de la dernière dépense pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible de récupérer la dernière dépense.", ex);
        }
    }
    
    public async Task AddExpenseAsync(int dogId, Expense expense)
    {
        ArgumentNullException.ThrowIfNull(expense);

        try
        {
            expense.DogId = dogId;
            expense.Date = expense.Date.ToUniversalTime();
            logger.LogInformation("Ajout de la dépense pour le chien {DogId}", dogId);
            context.Expenses.Add(expense);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Erreur lors de l'enregistrement de la dépense pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible d'enregistrer la dépense.", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur inattendue lors de l'ajout de la dépense pour le chien {DogId}", dogId);
            throw new ApplicationException("Une erreur est survenue lors de l'enregistrement de la dépense.", ex);
        }
    }
    
    public async Task DeleteExpenseAsync(int dogId, int expenseId)
    {
        try
        {
            var expense = await context.Expenses
                .Where(e => e.DogId == dogId && e.Id == expenseId)
                .FirstOrDefaultAsync();

            if (expense == null)
            {
                logger.LogError("Impossible de trouver la dépense {ExpenseId} pour le chien {DogId}", expenseId, dogId);
                throw new ApplicationException("Impossible de trouver la dépense.");
            }
            
            context.Expenses.Remove(expense);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la suppression de la dépense {ExpenseId} pour le chien {DogId}", expenseId, dogId);
            throw new ApplicationException("Impossible de supprimer la dépense.", ex);
        }
    }
}