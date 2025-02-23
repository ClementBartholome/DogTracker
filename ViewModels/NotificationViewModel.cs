namespace DogTracker.ViewModels;

public class NotificationViewModel
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime PlannedFor { get; set; }
    public string Content { get; set; } = string.Empty;
    public string MessageId { get; set; }
    public int? TreatmentId { get; set; }
    public string Title => TreatmentId.HasValue ? "Traitement" : "Notification";
}