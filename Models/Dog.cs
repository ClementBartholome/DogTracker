namespace DogTracker.Models;

public class Dog
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double CurrentWeight { get; set; }
    public List<WeightRecord> WeightHistory { get; set; } = new();
    public List<Walk> Walks { get; set; } = new();
    public List<Expense> Expenses { get; set; } = new();
}
