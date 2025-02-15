using DogTracker.Interfaces;
using DogTracker.Models;
using DogTracker.Services;
using DogTracker.ViewModels;
using Microsoft.AspNetCore.Components;

namespace DogTracker.Components.Pages
{
    public partial class DogDashboard
    {
        [Parameter] public int DogId { get; set; } = 1;
        [Inject] private IDogService DogService { get; set; } = null!;
        [Inject] private IWalkService WalkService { get; set; } = null!;
        [Inject] private IExpenseService ExpenseService { get; set; } = null!;
        
        private List<WalkViewModel> _recentWalks = [];
        private List<WeightRecord> _weightHistory = [];
        private List<Expense> _expenses = [];
        private string? _totalDurationToday;
        private double _totalDistanceToday;
        private int _totalWalksToday;
        private bool isLoading = false;
        private ExpenseSummaryViewModel expenseSummary = new();


        protected override async Task OnInitializedAsync()
        {
            isLoading = true;
            
            var walks = await WalkService.GetRecentWalksAsync(DogId);
            _recentWalks = walks.Select(w => new WalkViewModel(w)).ToList();
            
            _weightHistory = await DogService.GetWeightHistoryAsync(DogId);
            _expenses = await ExpenseService.GetExpensesAsync(DogId, DateTime.Now.Year, DateTime.Now.Month);
            CalculateTodayStats();
            PrepareExpenseSummary();
            isLoading = false;
        }
        
        private void PrepareExpenseSummary()
        {
            if (_expenses.Any())
            {
                expenseSummary = new ExpenseSummaryViewModel
                {
                    MonthlyTotal = _expenses
                        .Where(e => e.Date.Month == DateTime.Now.Month)
                        .Sum(e => e.Amount),
                    LastExpenseDate = _expenses
                        .OrderByDescending(e => e.Date)
                        .FirstOrDefault()?.Date
                        .AddHours(1)
                };
            }
            else
            {
                expenseSummary = new ExpenseSummaryViewModel();
            }
        }
        
        private void CalculateTodayStats()
        {
            var todayWalks = _recentWalks.Where(w => w.AdjustedStartTime.Date == DateTime.Today).ToList();
            
            if (todayWalks.Count == 0)
            {
                return;
            }
            
            var totalMinutes = todayWalks.Sum(w => (w.AdjustedEndTime - w.AdjustedStartTime).TotalMinutes);
            _totalDurationToday = totalMinutes < 60 ? $"{totalMinutes:F0} min" : $"{(int)totalMinutes / 60}h{totalMinutes % 60:00}";
            _totalDistanceToday = todayWalks.Sum(w => w.Distance);
            _totalWalksToday = todayWalks.Count;
        }
    }
}