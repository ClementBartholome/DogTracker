using DogTracker.Data;
using DogTracker.Interfaces;
using DogTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace DogTracker.Services;

public class TreatmentService(
    AppDbContext context,
    ILogger<TreatmentService> logger,
    NotificationService notificationService) : ITreatmentService
{
    public async Task<List<Treatment>> GetTreatmentsAsync(int dogId, int year, int month)
    {
        try
        {
            var startDate = new DateTime(year, month, 1).ToUniversalTime();
            var endDate = startDate.AddMonths(1).AddSeconds(-1).ToUniversalTime().AddHours(1);

            logger.LogInformation("Récupération des traitements pour le chien {DogId} du {StartDate} au {EndDate}",
                dogId, startDate, endDate);

            var treatments = await context.Treatments
                .Where(t => t.DogId == dogId && t.Date >= startDate && t.Date <= endDate)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            logger.LogInformation("Traitements récupérés avec succès pour le chien {DogId}", dogId);
            return treatments;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération des traitements pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible de récupérer les traitements.", ex);
        }
    }

    public async Task<Treatment> GetLastTreatment(int dogId)
    {
        try
        {
            logger.LogInformation("Récupération du dernier traitement pour le chien {DogId}", dogId);
            var treatment = await context.Treatments
                .Where(t => t.DogId == dogId)
                .OrderByDescending(t => t.Date)
                .FirstOrDefaultAsync();

            logger.LogInformation("Dernier traitement récupéré avec succès pour le chien {DogId}", dogId);
            return treatment;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération du dernier traitement pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible de récupérer le dernier traitement.", ex);
        }
    }

    public async Task AddTreatmentAsync(int dogId, Treatment treatment)
    {
        ArgumentNullException.ThrowIfNull(treatment);

        try
        {
            treatment.DogId = dogId;
            treatment.Date = treatment.Date.ToUniversalTime().AddHours(1);
            treatment.ReminderDate = treatment.ReminderDate?.ToUniversalTime().AddHours(1);
            context.Treatments.Add(treatment);
            await context.SaveChangesAsync();
            logger.LogInformation("Traitement ajouté avec succès pour le chien {DogId}", dogId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de l'ajout du traitement pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible d'ajouter le traitement.", ex);
        }
    }

    public async Task DeleteTreatmentAsync(int dogId, int treatmentId)
    {
        try
        {
            logger.LogInformation("Suppression du traitement {TreatmentId} pour le chien {DogId}", treatmentId, dogId);
            var treatment = await context.Treatments.FindAsync(treatmentId);
            if (treatment != null)
            {
                context.Treatments.Remove(treatment);

                var plannedNotification =
                    await context.Notifications.FirstOrDefaultAsync(n => n.TreatmentId == treatmentId);
                if (plannedNotification != null && plannedNotification.PlannedFor > DateTime.Now && plannedNotification.MessageId != null)
                {
                    await notificationService.DeleteScheduledNotificationAsync(plannedNotification?.MessageId!);
                }

                if (plannedNotification != null) context.Notifications.Remove(plannedNotification);
                await context.SaveChangesAsync();
                logger.LogInformation("Traitement {TreatmentId} supprimé avec succès pour le chien {DogId}",
                    treatmentId, dogId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la suppression du traitement {TreatmentId} pour le chien {DogId}",
                treatmentId, dogId);
            throw new ApplicationException("Impossible de supprimer le traitement.", ex);
        }
    }
}