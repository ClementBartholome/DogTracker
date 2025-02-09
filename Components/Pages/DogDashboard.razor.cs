using DogTracker.Models;
using DogTracker.ViewModels;
using Microsoft.AspNetCore.Components;

namespace DogTracker.Components.Pages
{
    public partial class DogDashboard
    {
        [Parameter] public int DogId { get; set; } = 1;
        
        private List<WalkViewModel> _recentWalks = [];
        private List<WeightRecord> _weightHistory = [];
        private List<Expense> _expenses = [];
        private string _totalDurationToday;
        private double _totalDistanceToday;
        private int _totalWalksToday;
        private bool isLoading = false;

        protected override async Task OnInitializedAsync()
        {
            isLoading = true;
            
            var walks = await WalkService.GetRecentWalksAsync(DogId);
            _recentWalks = walks.Select(w => new WalkViewModel(w)).ToList();
            
            _weightHistory = await DogService.GetWeightHistoryAsync(DogId);
            _expenses = await DogService.GetExpensesAsync(DogId, DateTime.Now.AddMonths(-1), DateTime.Now);
            CalculateTodayStats();
            isLoading = false;
        }
        
        private void CalculateTodayStats()
        {
            var todayWalks = _recentWalks.Where(w => w.AdjustedStartTime.Date == DateTime.Today).ToList();
            
            if (todayWalks.Count == 0)
            {
                return;
            }
            
            var totalMinutes = todayWalks.Sum(w => (w.AdjustedEndTime - w.AdjustedStartTime).TotalMinutes);
            _totalDurationToday = totalMinutes < 60 ? $"{totalMinutes:F0} min" : $"{totalMinutes / 60:F0}h{totalMinutes % 60:F0}";      
            _totalDistanceToday = todayWalks.Sum(w => w.Distance);
            _totalWalksToday = todayWalks.Count;
        }
    }
}