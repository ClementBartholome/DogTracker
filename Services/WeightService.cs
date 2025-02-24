using DogTracker.Data;
using DogTracker.Interfaces;
using DogTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace DogTracker.Services;

public class WeightService(AppDbContext context, ILogger<WeightService> logger) : IWeightService
{
    public async Task<List<WeightRecord>> GetWeightRecordsAsync(int dogId)
    {
        try
        {
            logger.LogInformation("Récupération de tous les poids pour le chien {DogId}", dogId);
        
            var weights = await context.WeightRecords
                .Where(w => w.DogId == dogId)
                .OrderByDescending(w => w.Date)
                .ToListAsync();
        
            logger.LogInformation("Poids récupérés avec succès pour le chien {DogId}", dogId);
            return weights;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération des poids pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible de récupérer les poids.", ex);
        }
    }

    public async Task<WeightRecord> GetLastWeightRecord(int dogId)
    {
        try
        {
            logger.LogInformation("Récupération du dernier poids pour le chien {DogId}", dogId);
            var weight = await context.WeightRecords
                .Where(w => w.DogId == dogId)
                .OrderByDescending(w => w.Date)
                .FirstOrDefaultAsync();
            
            logger.LogInformation("Dernier poids récupéré avec succès pour le chien {DogId}", dogId);
            return weight;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération du dernier poids pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible de récupérer le dernier poids.", ex);
        }
    }
    
    public async Task AddWeightRecordAsync(int dogId, WeightRecord weight)
    {
        ArgumentNullException.ThrowIfNull(weight);

        try
        {
            weight.DogId = dogId;
            weight.Date = weight.Date.ToUniversalTime().AddHours(2);
            context.WeightRecords.Add(weight);
            await context.SaveChangesAsync();
            logger.LogInformation("Poids ajouté avec succès pour le chien {DogId}", dogId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de l'ajout du poids pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible d'ajouter le poids.", ex);
        }
    }
    
    public async Task DeleteWeightRecordAsync(int dogId, int weightId)
    {
        try
        {
            logger.LogInformation("Suppression du poids {WeightId} pour le chien {DogId}", weightId, dogId);
            var weight = await context.WeightRecords.FindAsync(weightId);
            if (weight != null)
            {
                context.WeightRecords.Remove(weight);
                await context.SaveChangesAsync();
                logger.LogInformation("Poids {WeightId} supprimé avec succès pour le chien {DogId}", weightId, dogId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la suppression du poids {WeightId} pour le chien {DogId}", weightId, dogId);
            throw new ApplicationException("Impossible de supprimer le poids.", ex);
        }
    }
}