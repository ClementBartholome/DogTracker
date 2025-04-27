using DogTracker.Extensions;
using DogTracker.Interfaces;
using DogTracker.Models;
using DogTracker.Services;
using DogTracker.ViewModels;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DogTracker.Components.Pages
{
    public partial class DogDashboard
    {
        [Parameter] public int DogId { get; set; } = 1;
        [Inject] private IWeightService WeightService { get; set; } = null!;
        [Inject] private IWalkService WalkService { get; set; } = null!;
        [Inject] private IExpenseService ExpenseService { get; set; } = null!;
        [Inject] private ILocationService LocationService { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!;

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
        private bool isTracking;
        private DateTime? startTime;
        private Timer timer;


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

            var ongoingWalkStartTime = await LocationService.CheckForOngoingUntrackedWalkAsync();
            if (ongoingWalkStartTime != DateTime.MinValue)
            {
                isTracking = true;
                startTime = ongoingWalkStartTime.ToLocalTime();
                timer = new Timer(_ => { InvokeAsync(StateHasChanged); }, null, 0, 1000);
                Snackbar.Add("Reprise de la promenade", Severity.Info);
            }

            isLoading = false;
        }
        //
        // protected override async Task OnAfterRenderAsync(bool firstRender)
        // {
        //     if (!firstRender)
        //     {
        //         
        //     }
        // }

        private async Task StartWalkNoTracking()
        {
            isTracking = true;
            startTime = DateTime.Now;

            await LocationService.StartTimer();

            Snackbar.Add("Promenade lancée !", Severity.Success);

            timer = new Timer(_ => { InvokeAsync(StateHasChanged); }, null, 0, 1000);
        }

        private async Task StopWalkNoTracking()
        {
            try
            {
                isTracking = false;

                await LocationService.StopUntrackedWalkAsync();

                var walk = new Walk
                {
                    StartTime = startTime!.Value,
                    EndTime = DateTime.Now,
                    Distance = 0,
                };

                await WalkService.AddWalkAsync(DogId, walk);

                Snackbar.Add("Promenade enregistrée !", Severity.Success);
                await timer.DisposeAsync();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Erreur lors de l'enregistrement de la promenade : {ex.Message}", Severity.Error);
            }
        }

        private string GetFormattedDuration()
        {
            if (!startTime.HasValue) return "00:00:00";
            var duration = DateTime.Now - startTime.Value;
            return duration.ToString(@"hh\:mm\:ss");
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
                var franceTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");

                var utcDate = _expenses
                    .OrderByDescending(e => e.Date)
                    .FirstOrDefault()?.Date;

                DateTime? convertedDate = utcDate.HasValue
                    ? TimeZoneInfo.ConvertTimeFromUtc(utcDate.Value.ToUniversalTime(), franceTimeZone)
                    : null;

                expenseSummary = new ExpenseSummaryViewModel
                {
                    YearTotal = _expenses
                        .Sum(e => e.Amount),
                    LastExpenseDate = convertedDate,
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
            _totalDurationToday = totalMinutes < 60
                ? $"{totalMinutes:F0} min"
                : $"{(int)totalMinutes / 60}h{totalMinutes % 60:00}";
            _totalDistanceToday = todayWalks.Sum(w => w.Distance);
            _totalWalksToday = todayWalks.Count;
        }
    }
}