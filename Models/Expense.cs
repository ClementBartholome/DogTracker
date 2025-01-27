using DogTracker.Enums;

namespace DogTracker.Models;

public class Expense
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public CategoryEnum Category { get; set; } // Nourriture, Vétérinaire, Accessoires, etc.
    public string? Description { get; set; }
    public int DogId { get; set; }
}