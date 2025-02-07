using DogTracker.Models;
using Microsoft.AspNetCore.Components;

namespace DogTracker.Components.Pages
{
    public partial class DogDashboard
    {
        [Parameter] public int DogId { get; set; } = 1;
        
        private List<Walk?> _recentWalks = [];
        private List<WeightRecord> _weightHistory = [];
        private List<Expense> _expenses = [];
        private double _totalDurationToday;
        private double _totalDistanceToday;
        private int _totalWalksToday;
        private bool isLoading = false;

        protected override async Task OnInitializedAsync()
        {
            isLoading = true;
            _recentWalks = await WalkService.GetRecentWalksAsync(DogId);
            _weightHistory = await DogService.GetWeightHistoryAsync(DogId);
            _expenses = await DogService.GetExpensesAsync(DogId, DateTime.Now.AddMonths(-1), DateTime.Now);
            CalculateTodayStats();
            isLoading = false;
        }
        
        private void CalculateTodayStats()
        {
            var todayWalks = _recentWalks.Where(w => w.StartTime.Date == DateTime.Today).ToList();
            
            if (todayWalks.Count == 0)
            {
                return;
            }
            
            _totalDurationToday = todayWalks.Sum(w => (w.EndTime - w.StartTime).TotalMinutes);
            _totalDistanceToday = todayWalks.Sum(w => w.Distance);
            _totalWalksToday = todayWalks.Count;
        }
    }
}