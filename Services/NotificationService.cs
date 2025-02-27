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

        var notification = new
        {
            app_id = AppId,
            target_channel = "push",
            included_segments = new[] { "Total Subscriptions" },
            contents = new
            {
                en = $"Reminder : {treatment.Name} treatment is due",
                fr = $"N'oublie pas le traitement {treatment.Name} !"
            },
            url = "https://montoutou-h0bdfdhndcg4dseg.westeurope-01.azurewebsites.net/dog/1/carnet-sante",
            send_after = treatment.ReminderDate.Value.Date.AddHours(16).AddMinutes(40)
                .ToString("yyyy-MM-dd'T'HH:mm:ssZ")
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(OneSignalApiUrl, notification, ctk);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync(ctk);
            var responseData = JsonSerializer.Deserialize<OneSignalResponse>(responseContent);

            var notificationEntity = new Notification
            {
                CreatedAt = DateTime.UtcNow,
                PlannedFor = treatment.ReminderDate.Value.Date.AddHours(16).AddMinutes(40),
                Content = notification.contents.fr,
                MessageId = responseData?.Id!,
                TreatmentId = treatment.Id,
            };

            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Notifications.Add(notificationEntity);
            await dbContext.SaveChangesAsync(ctk);

            logger.LogInformation("Reminder notification scheduled for treatment {TreatmentName} on {ReminderDate}",
                treatment.Name, treatment.ReminderDate.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error scheduling reminder notification for treatment {TreatmentName}", treatment.Name);
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

            
            var notifications = await dbContext.Notifications
                .Where(n => n.PlannedFor.Date <= utcDateToLocal.Date && n.IsDone == false)
                .ToListAsync(ctk);
            
            var notificationsViewModel = notifications.Select(n => new NotificationViewModel
            {
                Id = n.Id,
                TreatmentId = n.TreatmentId,
                Content = n.Content,
                IsDone = n.IsDone,
            }).ToList();
            logger.LogInformation("Notifications du jour récupérées avec succès");
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
}