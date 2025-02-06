using DogTracker.Interfaces;
using DogTracker.Models;
using DogTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace DogTracker.Services;

public class DogService(AppDbContext context, ILogger<DogService> logger) : IDogService
{
    public async Task<List<Walk?>> GetRecentWalksAsync(int dogId)
    {
        try
        {
            logger.LogInformation("Récupération des promenades pour le chien {DogId}", dogId);
            var walks = await context.Walks
                .Where(w => w!.DogId == dogId)
                .OrderByDescending(w => w!.StartTime)
                .ToListAsync();
            logger.LogInformation("Promenades récupérées avec succès pour le chien {DogId}", dogId);
            return walks;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération des promenades pour le chien {DogId}", dogId);
            throw; 
        }
    }

    public async Task AddWalkAsync(int dogId, Walk? walk)
    {
        ArgumentNullException.ThrowIfNull(walk);

        try
        {
            walk.DogId = dogId;
            walk.StartTime = walk.StartTime.ToUniversalTime();
            walk.EndTime = walk.EndTime.ToUniversalTime();
            
            context.Walks.Add(walk);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Erreur lors de l'enregistrement de la promenade pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible d'enregistrer la promenade.", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur inattendue lors de l'ajout de la promenade pour le chien {DogId}", dogId);
            throw new ApplicationException("Une erreur est survenue lors de l'enregistrement de la promenade.", ex);
        }
    }

    public async Task<List<WeightRecord>> GetWeightHistoryAsync(int dogId)
    {
        try
        {
            return await context.WeightRecords
                .Where(w => w.DogId == dogId)
                .OrderByDescending(w => w.Date)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération de l'historique du poids pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible de récupérer l'historique du poids.", ex);
        }
    }

    public async Task AddWeightRecordAsync(int dogId, WeightRecord weight)
    {
        if (weight == null)
        {
            throw new ArgumentNullException(nameof(weight));
        }

        try
        {
            weight.DogId = dogId;
            context.WeightRecords.Add(weight);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Erreur lors de l'enregistrement du poids pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible d'enregistrer le poids.", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur inattendue lors de l'ajout du poids pour le chien {DogId}", dogId);
            throw new ApplicationException("Une erreur est survenue lors de l'enregistrement du poids.", ex);
        }
    }

    public async Task<List<Expense>> GetExpensesAsync(int dogId, DateTime startDate, DateTime endDate)
    {
        try
        {
            startDate = startDate.ToUniversalTime();
            endDate = endDate.ToUniversalTime();
            return await context.Expenses
                .Where(e => e.DogId == dogId && e.Date >= startDate && e.Date <= endDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération des dépenses pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible de récupérer les dépenses.", ex);
        }
    }

    public async Task AddExpenseAsync(int dogId, Expense expense)
    {
        if (expense == null)
        {
            throw new ArgumentNullException(nameof(expense));
        }

        try
        {
            expense.DogId = dogId;
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
    
    public async Task AddDogAsync(Dog dog)
    {
        if (dog == null)
        {
            throw new ArgumentNullException(nameof(dog));
        }

        try
        {
            context.Dogs.Add(dog);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Erreur lors de l'enregistrement du chien");
            throw new ApplicationException("Impossible d'enregistrer le chien.", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur inattendue lors de l'ajout du chien");
            throw new ApplicationException("Une erreur est survenue lors de l'enregistrement du chien.", ex);
        }
    }
    
    public async Task DeleteWalkAsync(int dogId, int walkId)
    {
        try
        {
            var walk = await context.Walks.FindAsync(walkId);
            if (walk == null)
            {
                logger.LogError("Impossible de trouver la promenade {WalkId}", walkId);
                throw new ApplicationException("Impossible de trouver la promenade.");
            }

            if (walk.DogId != dogId)
            {
                logger.LogError("Impossible de supprimer la promenade {WalkId} pour le chien {DogId}", walkId, dogId);
                throw new ApplicationException("Impossible de supprimer la promenade.");
            }

            context.Walks.Remove(walk);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Erreur lors de la suppression de la promenade {WalkId} pour le chien {DogId}", walkId, dogId);
            throw new ApplicationException("Impossible de supprimer la promenade.", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur inattendue lors de la suppression de la promenade {WalkId} pour le chien {DogId}", walkId, dogId);
            throw new ApplicationException("Une erreur est survenue lors de la suppression de la promenade.", ex);
        }
    }
    
    public async Task<Walk?> GetWalkByIdAsync(int walkId)
    {
        try
        {
            return await context.Walks.FindAsync(walkId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération de la promenade {WalkId}", walkId);
            throw new ApplicationException("Impossible de récupérer la promenade.", ex);
        }
    }
}