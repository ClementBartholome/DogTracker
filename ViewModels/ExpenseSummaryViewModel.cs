namespace DogTracker.ViewModels
{
    public class ExpenseSummaryViewModel
    {
        public decimal MonthlyTotal { get; set; } = 0;
        public decimal QuarterTotal { get; set; } = 0;
        public DateTime? LastExpenseDate { get; set; } 
    }
}