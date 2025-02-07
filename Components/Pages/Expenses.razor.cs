// using Microsoft.AspNetCore.Components;
// using DogTracker.Models;
// using DogTracker.Services;
// using System.Threading.Tasks;
// using System.Collections.Generic;
// using System.Linq;
//
// namespace DogTracker.Components.Pages
// {
//     public partial class Expenses : ComponentBase
//     {
//         [Inject] private IExpenseService ExpenseService { get; set; }
//         [Inject] private IJSRuntime JsRuntime { get; set; }
//
//         private bool isLoading = true;
//         private List<Expense> expenseHistory;
//         private DateTime _selectedMonth = DateTime.Now;
//
//         protected override async Task OnInitializedAsync()
//         {
//             await LoadExpenses();
//         }
//
//         private async Task LoadExpenses()
//         {
//             isLoading = true;
//             expenseHistory = await ExpenseService.GetExpensesAsync();
//             isLoading = false;
//         }
//
//         private async Task OnMonthChanged(DateTime newDate)
//         {
//             _selectedMonth = newDate;
//             await LoadExpenses();
//         }
//
//         private async Task AddExpense()
//         {
//             // Logic to add a new expense
//         }
//
//         private async Task DeleteExpense(int expenseId)
//         {
//             await ExpenseService.DeleteExpenseAsync(expenseId);
//             await LoadExpenses();
//         }
//     }
// }