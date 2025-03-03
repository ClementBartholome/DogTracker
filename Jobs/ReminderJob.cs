using DogTracker.Services;
using Quartz;

namespace DogTracker.Jobs;

public class ReminderJob(ILogger<ReminderJob> logger, NotificationService notificationService) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try 
        {
            await notificationService.ProcessPendingNotifications(context.CancellationToken);
            // await notificationService.SendNotifications(context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing notification job");
            throw; 
        }
    }
}