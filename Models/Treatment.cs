using DogTracker.Enums;

namespace DogTracker.Models;

public class Treatment
{
    public int Id { get; set; }
    public string Name { get; set; }
    public TreatmentTypeEnum Type { get; set; }
    public string? Comment { get; set; }
    public DateTime? ReminderDate { get; set; }
    public DateTime Date { get; set; }
    public int DogId { get; set; }
}