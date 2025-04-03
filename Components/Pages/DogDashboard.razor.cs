using DogTracker.Extensions;
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
        [Inject] private IWeightService WeightService { get; set; } = null!;
        [Inject] private IWalkService WalkService { get; set; } = null!;
        [Inject] private IExpenseService ExpenseService { get; set; } = null!;
        
        private List<WalkViewModel> _recentWalks = [];
        private WeightRecord _currentWeight = new();
        private List<Expense> _expenses = [];
        private string? _totalDurationToday;
        private double _totalDistanceToday;
        private int _totalWalksToday;
        private bool isLoading;
        private ExpenseSummaryViewModel expenseSummary = new();
        private string? _weeklyAverageDuration;
        private double _weeklyAverageDistance;
        private int _totalWalksWeek;
        private string _selectedTimePeriod = "today";


        protected override async Task OnInitializedAsync()
        {
            isLoading = true;
            
            var walks = await WalkService.GetRecentWalksAsync(DogId);
            _recentWalks = walks.Select(w => new WalkViewModel(w)).ToList();
            
            _currentWeight = await WeightService.GetLastWeightRecord(DogId);
            _expenses = await ExpenseService.GetYearlyExpenses(DogId, DateTime.Now.Year);
            CalculateTodayStats();
            CalculateWeeklyStats(); 
            PrepareExpenseSummary();
            isLoading = false;
        }
        
        private void CalculateWeeklyStats()
        {
            // Get start of week (Monday)
            DateTime startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            if ((int)DateTime.Today.DayOfWeek == 0) // Sunday is 0
                startOfWeek = startOfWeek.AddDays(-7);
    
            var weekWalks = _recentWalks
                .Where(w => w.AdjustedStartTime.Date >= startOfWeek && 
                            w.AdjustedStartTime.Date <= DateTime.Today)
                .ToList();
    
            _totalWalksWeek = weekWalks.Count;
    
            if (_totalWalksWeek == 0)
            {
                _weeklyAverageDuration = "0 min";
                _weeklyAverageDistance = 0;
                return;
            }
    
            // Calculate days elapsed in current week
            int daysElapsed = Math.Max(1, (int)(DateTime.Today - startOfWeek).TotalDays + 1);
    
            // Calculate average minutes per day
            var totalMinutes = weekWalks.Sum(w => (w.AdjustedEndTime - w.AdjustedStartTime).TotalMinutes);
            var avgMinutesPerDay = totalMinutes / daysElapsed;
    
            _weeklyAverageDuration = avgMinutesPerDay < 60 
                ? $"{avgMinutesPerDay:F0} min" 
                : $"{(int)avgMinutesPerDay / 60}h{avgMinutesPerDay % 60:00}";
    
            _weeklyAverageDistance = weekWalks.Sum(w => w.Distance) / daysElapsed;
        }
        
        private void PrepareExpenseSummary()
        {
            if (_expenses.Count != 0)
            {
                
                expenseSummary = new ExpenseSummaryViewModel
                {
                    YearTotal = _expenses
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