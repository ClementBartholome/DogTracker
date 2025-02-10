using DogTracker.Interfaces;
using DogTracker.Models;
using DogTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace DogTracker.Services;

public class DogService(AppDbContext context, ILogger<DogService> logger) : IDogService
{
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
}