using System.Text;
using System.Text.Json;
using DogTracker.Data;
using DogTracker.Models;
using DogTracker.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace DogTracker.Services;

public class NotificationService(
    IConfiguration configuration,
    IServiceProvider serviceProvider,
    ILogger<NotificationService> logger,
    IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("OneSignalClient");

    private readonly string? AppId = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production"
        ? Environment.GetEnvironmentVariable("APPSETTING_OneSignalAppId")
        : configuration["OneSignal:AppId:Test"];

    private const string OneSignalApiUrl = "https://api.onesignal.com/notifications";


    public async Task SendNotifications(CancellationToken ctk = default)
    {
        try
        {
            await SendOneSignalNotification(ctk);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de l'envoi des notifications");
            throw;
        }
    }

    private async Task SendOneSignalNotification(CancellationToken ctk = default)
    {
        var notification = new
        {
            app_id = AppId,
            target_channel = "push",
            included_segments = new[] { "Total Subscriptions" },
            contents = new
            {
                en = "",
                fr = "",
            },
            url = "https://montoutou-h0bdfdhndcg4dseg.westeurope-01.azurewebsites.net/dog/1/carnet-sante",
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(OneSignalApiUrl, notification, ctk);
            response.EnsureSuccessStatusCode();
            logger.LogInformation("OneSignal notification sent successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de l'envoi de la notification OneSignal");
            throw;
        }
    }

    public async Task ScheduleReminderNotification(Treatment treatment, CancellationToken ctk = default)
    {
        if (!treatment.ReminderDate.HasValue) return;

        var reminderDateTime = treatment.ReminderDate.Value.Date.AddHours(16).AddMinutes(40);
        var oneMonthFromNow = DateTime.UtcNow.AddMonths(1);
        
        var notificationEntity = new Notification
        {
            CreatedAt = DateTime.UtcNow,
            PlannedFor = reminderDateTime,
            Content = $"N'oublie pas le traitement {treatment.Name} !",
            TreatmentId = treatment.Id,
        };

        // OneSignal ne permet pas de planifier une notif > 1 mois dans le futur
        if (reminderDateTime <= oneMonthFromNow)
        {
            try
            {
                var notification = new
                {
                    app_id = AppId,
                    target_channel = "push",
                    included_segments = new[] { "Total Subscriptions" },
                    contents = new
                    {
                        en = $"Reminder: {treatment.Name} treatment is due",
                        fr = notificationEntity.Content
                    },
                    url = "https://montoutou-h0bdfdhndcg4dseg.westeurope-01.azurewebsites.net/dog/1/carnet-sante",
                    send_after = reminderDateTime.ToString("yyyy-MM-dd'T'HH:mm:ssZ")
                };

                var response = await _httpClient.PostAsJsonAsync(OneSignalApiUrl, notification, ctk);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync(ctk);
                var responseData = JsonSerializer.Deserialize<OneSignalResponse>(responseContent);
                
                notificationEntity.MessageId = responseData?.Id;
                
                await SaveNotificationInDatabase(notificationEntity, ctk);

                logger.LogInformation(
                    "Rappel pour le traitement {TreatmentName} programmé via OneSignal pour le {ReminderDate}",
                    treatment.Name, reminderDateTime);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Erreur lors de l'envoi de la notification OneSignal pour le traitement {TreatmentName}",
                    treatment.Name);
                throw;
            }
        }
        else
        {
            logger.LogInformation(
                "Notification pour le traitement {TreatmentName} prévue pour {ReminderDate} est trop loin dans le futur. Sauvegarde en base uniquement.",
                treatment.Name, reminderDateTime);
        }
        
        await SaveNotificationInDatabase(notificationEntity, ctk);
    }
    
    public async Task ProcessPendingNotifications(CancellationToken ctk = default)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var pendingNotifications = await dbContext.Notifications
                .Where(n => n.MessageId == null && n.PlannedFor <= DateTime.UtcNow.AddMonths(1))
                .AsSplitQuery()
                .Include(n => n.Treatment)
                .ToListAsync(ctk);

            foreach (var notification in pendingNotifications)
            {
                await SchedulePendingNotifications(notification, ctk);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la planification des notifications en attente");
            throw;
        }
    }
    
    private async Task SchedulePendingNotifications(Notification notification, CancellationToken ctk = default)
    {
        try
        {
            var oneSignalNotification = new
            {
                app_id = AppId,
                target_channel = "push",
                included_segments = new[] { "Total Subscriptions" },
                contents = new
                {
                    en = $"Reminder: {notification.Treatment.Name} treatment is due",
                    fr = notification.Content
                },
                url = "https://montoutou-h0bdfdhndcg4dseg.westeurope-01.azurewebsites.net/dog/1/carnet-sante",
                send_after = notification.PlannedFor.ToString("yyyy-MM-dd'T'HH:mm:ssZ")
            };

            var response = await _httpClient.PostAsJsonAsync(OneSignalApiUrl, oneSignalNotification, ctk);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync(ctk);
            
            var responseData = JsonSerializer.Deserialize<OneSignalResponse>(responseContent);
            
            notification.MessageId = responseData?.Id;
            
            await SaveNotificationInDatabase(notification, ctk);
            
            logger.LogInformation("Notification {NotificationId} programmée via OneSignal avec succès", notification.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la planification de la notification {NotificationId}", notification.Id);
            throw;
        }
    }
    
    private async Task SaveNotificationInDatabase(Notification notification, CancellationToken ctk = default)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            dbContext.Notifications.Add(notification);
            await dbContext.SaveChangesAsync(ctk);
            logger.LogInformation("Notification {NotificationId} saved in database", notification.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving notification in database");
            throw;
        }
    }


    public async Task DeleteScheduledNotificationAsync(string messageId, CancellationToken ctk = default)
    {
        try
        {
            var url = $"{OneSignalApiUrl}/{messageId}?app_id={AppId}";
            var request = new HttpRequestMessage(HttpMethod.Delete, url);

            var response = await _httpClient.SendAsync(request, ctk);
            response.EnsureSuccessStatusCode();
            logger.LogInformation("Scheduled notification {MessageId} deleted successfully", messageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting scheduled notification {MessageId}", messageId);
            throw;
        }
    }

    public async Task<List<NotificationViewModel>> GetNotifications(CancellationToken ctk = default)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var utcDateToLocal = DateTime.UtcNow.AddHours(1);


            var startDate = utcDateToLocal.Date;
            var endDate = utcDateToLocal.Date.AddDays(90);

            var notifications = await dbContext.Notifications
                .Where(n => n.PlannedFor.Date >= startDate && n.PlannedFor.Date <= endDate && n.IsDone == false)
                .ToListAsync(ctk);

            var notificationsViewModel = notifications.Select(n => new NotificationViewModel
            {
                Id = n.Id,
                TreatmentId = n.TreatmentId,
                Content = n.Content,
                IsDone = n.IsDone,
                PlannedFor = n.PlannedFor,
            }).ToList();
            logger.LogInformation("Notifications récupérées avec succès");
            return notificationsViewModel;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération des notifications du jour");
            throw;
        }
    }

    public async Task MarkNotificationAsDone(int notificationId, CancellationToken ctk = default)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var notification = await dbContext.Notifications.FindAsync([notificationId], ctk);
            if (notification != null)
            {
                notification.IsDone = true;
                await dbContext.SaveChangesAsync(ctk);
                logger.LogInformation("Notification {NotificationId} marked as done", notificationId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking notification {NotificationId} as done", notificationId);
            throw;
        }
    }

    public async Task<List<NotificationViewModel>> GetAllNotifications(CancellationToken ctk = default)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var notifications = await dbContext.Notifications
                .OrderByDescending(n => n.PlannedFor)
                .ToListAsync(ctk);

            return notifications.Select(n => new NotificationViewModel
            {
                Id = n.Id,
                TreatmentId = n.TreatmentId,
                Content = n.Content,
                IsDone = n.IsDone,
                PlannedFor = n.PlannedFor,
                CreatedAt = n.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération des notifications");
            throw;
        }
    }

    public async Task DeleteNotificationAsync(int notificationId, CancellationToken ctk = default)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var notification = await dbContext.Notifications.FindAsync([notificationId], ctk);
            if (notification != null)
            {
                dbContext.Notifications.Remove(notification);
                await dbContext.SaveChangesAsync(ctk);

                // S'il y a une notif OneSignal associée, on envoie la requête pour la supprimer aussi
                if (!string.IsNullOrEmpty(notification.MessageId))
                {
                    await DeleteScheduledNotificationAsync(notification.MessageId, ctk);
                }

                logger.LogInformation("Notification {NotificationId} deleted", notificationId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting notification {NotificationId}", notificationId);
            throw;
        }
    }
}