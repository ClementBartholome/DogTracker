using System.Text;
using System.Text.Json;
using DogTracker.Data;
using DogTracker.Models;

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


    public async Task SendNotifications(CancellationToken ctk)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await SendOneSignalNotification(ctk);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de l'envoi des notifications");
            throw;
        }
    }

    private async Task SendOneSignalNotification(CancellationToken ctk)
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

    public async Task ScheduleReminderNotification(Treatment treatment, CancellationToken ctk)
    {
        if (!treatment.ReminderDate.HasValue) return;

        var notification = new
        {
            app_id = AppId,
            target_channel = "push",
            included_segments = new[] { "Total Subscriptions" },
            contents = new
            {
                en = $"Reminder : {treatment.Name} treatment is due today",
                fr = $"N'oublie pas le traitement {treatment.Name} aujourd'hui !"
            },
            url = "https://montoutou-h0bdfdhndcg4dseg.westeurope-01.azurewebsites.net/dog/1/carnet-sante",
            send_after = treatment.ReminderDate.Value.Date.AddHours(17).AddMinutes(40)
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
                PlannedFor = treatment.ReminderDate.Value.Date.AddHours(17).AddMinutes(40),
                Content = notification.contents.fr,
                MessageId = responseData?.Id!,
                TreatmentId = treatment.Id
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
}