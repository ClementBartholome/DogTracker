using DogTracker.Components.Dialog;
using Microsoft.AspNetCore.Components;
using DogTracker.Models;
using DogTracker.Interfaces;
using DogTracker.ViewModels;
using MudBlazor;

namespace DogTracker.Components.Pages
{
    public partial class Expenses : ComponentBase
    {
        [Parameter] public int dogId { get; set; } = 1;
        [Inject] private IExpenseService ExpenseService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; } = null!;
        [Inject] private IDialogService DialogService { get; set; } = null!;

        private bool isLoading = true;
        private List<Expense> expenseHistory;
        private List<Expense> yearlyExpenses;
        private DateTime? _selectedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        private double[] chartData;
        private string[] labels;
        private ExpenseSummaryViewModel expenseSummary = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadExpenses();
        }

        private async Task LoadExpenses()
        {
            isLoading = true;

            // Load monthly expenses for the history list
            expenseHistory = await ExpenseService.GetExpensesByMonth(dogId, _selectedMonth?.Year ?? DateTime.Now.Year,
                _selectedMonth?.Month ?? DateTime.Now.Month);

            // Load quarterly expenses for the chart and summary
            yearlyExpenses = await ExpenseService.GetYearlyExpenses(dogId, DateTime.Now.Year);

            PrepareChartData();
            PrepareExpenseSummary();
            isLoading = false;
        }

        private void PrepareExpenseSummary()
        {
            var yearlyTotal = yearlyExpenses.Sum(e => e.Amount);

            if (expenseHistory.Count != 0)
            {
                expenseSummary = new ExpenseSummaryViewModel
                {
                    MonthlyTotal = expenseHistory.Sum(e => e.Amount),
                    LastExpenseDate = expenseHistory
                        .OrderByDescending(e => e.Date)
                        .FirstOrDefault()?.Date
                        .ToLocalTime(),
                    YearTotal = yearlyTotal

                };
            }
            else
            {
                expenseSummary = new ExpenseSummaryViewModel
                {
                    MonthlyTotal = 0,
                    LastExpenseDate = null,
                    YearTotal = yearlyTotal
                };
            }
        }

        private void PrepareChartData()
        {
            // Use quarterly data for the chart
            var groupedExpenses = yearlyExpenses
                .GroupBy(e => e.Category)
                .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
                .ToList();

            chartData = groupedExpenses.Select(g => (double)g.Total).ToArray();

            var totalAmount = yearlyExpenses.Count != 0 ? yearlyExpenses.Sum(e => e.Amount) : 1;
            labels = groupedExpenses.Select(g =>
                    $"{g.Category} - {g.Total.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("fr-FR"))} ({(g.Total / totalAmount * 100):F0}%)")
                .ToArray();
        }

        private async Task OnMonthChanged(DateTime? newMonth)
        {
            _selectedMonth = newMonth;
            if (_selectedMonth.HasValue)
            {
                var startDate = new DateTime(_selectedMonth.Value.Year, _selectedMonth.Value.Month, 1);
                expenseHistory = await ExpenseService.GetExpensesByMonth(dogId, startDate.Year, startDate.Month);
            }

            StateHasChanged();
        }

        private async Task OpenAddExpenseDialog()
        {
            var parameters = new DialogParameters
            {
            };
            var dialog = await DialogService.ShowAsync<AddExpenseDialog>("Ajouter une dépense", parameters);
            var result = await dialog.Result;
            if (result is { Canceled: false, Data: Expense expense })
            {
                await AddExpense(expense);
            }
        }

        private async Task AddExpense(Expense expense)
        {
            try
            {
                isLoading = true;
                await ExpenseService.AddExpenseAsync(dogId, expense);
                await LoadExpenses();
                Snackbar.Add("Dépense ajoutée !", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add("Erreur lors de l'ajout de la dépense.", Severity.Error);
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task ConfirmDeleteExpense(int expenseId)
        {
            var parameters = new DialogParameters
            {
                { "ContentText", "Es-tu sûr de vouloir supprimer cette dépense ?" },
                { "ButtonText", "Supprimer" },
                { "Color", Color.Error }
            };

            var dialog = await DialogService.ShowAsync<Dialog.Dialog>("Confirmation", parameters);
            var result = await dialog.Result;
            if (!result.Canceled)
            {
                await DeleteExpense(expenseId);
            }
        }

        private async Task DeleteExpense(int expenseId)
        {
            try
            {
                isLoading = true;
                await ExpenseService.DeleteExpenseAsync(dogId, expenseId);
                await LoadExpenses();
                Snackbar.Add("Dépense supprimée !", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add("Erreur lors de la suppression de la dépense.", Severity.Error);
            }
            finally
            {
                isLoading = false;
            }
        }
    }
}