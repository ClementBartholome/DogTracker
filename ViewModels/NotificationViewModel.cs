namespace DogTracker.ViewModels;

public class NotificationViewModel
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? TreatmentId { get; set; }
    public bool IsDone { get; set; }
    public string Title => TreatmentId.HasValue ? "Traitement" : "Notification";
    public DateTime CreatedAt { get; set; }
    public DateTime PlannedFor { get; set; }
}