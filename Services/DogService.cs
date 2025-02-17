using DogTracker.Interfaces;
using DogTracker.Models;
using DogTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace DogTracker.Services;

public class DogService(AppDbContext context, ILogger<DogService> logger) : IDogService
{
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