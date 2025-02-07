using DogTracker.Interfaces;
using DogTracker.Models;
using DogTracker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DogTracker.Services
{
    public class WalkService(AppDbContext context, ILogger<WalkService> logger) : IWalkService
    {
        public async Task<List<Walk?>> GetWalksByMonthAsync(int dogId, int year, int month)
        {
            try
            {
                var startDate = new DateTime(year, month, 1).ToUniversalTime();
                var endDate = startDate.AddMonths(1).AddSeconds(-1).ToUniversalTime().AddHours(1);
                
                logger.LogInformation("Récupération des promenades pour le chien {DogId} du {StartDate} au {EndDate}", dogId, startDate, endDate);
                var walks = await context.Walks
                    .Where(w => w!.DogId == dogId && w.StartTime >= startDate && w.StartTime <= endDate)
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
}