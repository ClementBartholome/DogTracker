using DogTracker.Models;
using DogTracker.Services;
using Microsoft.AspNetCore.Components;

namespace DogTracker.Components.Pages
{
    public partial class DogDashboard
    {
        [Parameter] public int DogId { get; set; }
        
        private List<Walk> recentWalks = new();
        private List<WeightRecord> weightHistory = new();
        private List<Expense> expenses = new();


        protected override async Task OnInitializedAsync()
        {
            recentWalks = await DogService.GetRecentWalksAsync(DogId);
            weightHistory = await DogService.GetWeightHistoryAsync(DogId);
            expenses = await DogService.GetExpensesAsync(DogId, DateTime.Now.AddMonths(-1), DateTime.Now);
        }
    }
}