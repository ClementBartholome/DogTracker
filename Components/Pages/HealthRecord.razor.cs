using DogTracker.Components.Dialog;
using DogTracker.Interfaces;
using DogTracker.Models;
using DogTracker.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DogTracker.Components.Pages;

public partial class HealthRecord : ComponentBase
{
    [Inject] private ITreatmentService TreatmentService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;
    private bool isLoading = true;
    private DateTime? _selectedMonth = DateTime.Today;
    private List<Treatment>? treatments;
    [Parameter] public int dogId { get; set; } = 1;

    protected override async Task OnInitializedAsync()
    {
        await LoadTreatments();
    }
    
    private async Task LoadTreatments()
    {
        isLoading = true;
        treatments = await TreatmentService.GetTreatmentsAsync(dogId, _selectedMonth?.Year ?? DateTime.Now.Year, _selectedMonth?.Month ?? DateTime.Now.Month);
        isLoading = false;
    }
    
    private async Task OpenAddTreatmentDialog()
    {
        var parameters = new DialogParameters { };
        var dialog = DialogService.Show<AddTreatmentDialog>("Ajouter un traitement", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false, Data: Treatment treatment })
        {
            await AddTreatment(treatment);
        }
    }

    private async Task AddTreatment(Treatment treatment)
    {
        try
        {
            isLoading = true;
            await TreatmentService.AddTreatmentAsync(dogId, treatment);
        
            if (treatment.ReminderDate.HasValue)
            {
                await NotificationService.ScheduleReminderNotification(treatment, CancellationToken.None);
            }
        
            await LoadTreatments();
            Snackbar.Add("Traitement ajouté avec succès", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add("Erreur lors de l'ajout du traitement", Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OnMonthChanged(DateTime? date)
    {
        if (date.HasValue)
        {
            _selectedMonth = date;
            if (_selectedMonth.HasValue)
            {
                var startDate = new DateTime(_selectedMonth.Value.Year, _selectedMonth.Value.Month, 1);
                treatments = await TreatmentService.GetTreatmentsAsync(dogId, startDate.Year, startDate.Month);
            }

            StateHasChanged();
        }
    }

    private async Task ConfirmDeleteTreatment(int treatmentId)
    {
        var parameters = new DialogParameters
        {
            { "ContentText", "Es-tu sûr de vouloir supprimer ce traitement ?" },
            { "ButtonText", "Supprimer" },
            { "Color", Color.Error }
        };

        var dialog = await DialogService.ShowAsync<Dialog.Dialog>("Confirmation", parameters);
        var result = await dialog.Result;
        if (!result.Canceled)
        {
            await DeleteTreatment(treatmentId);
        }
    }

    private async Task DeleteTreatment(int id)
    {
        try
        {
            isLoading = true;
            await TreatmentService.DeleteTreatmentAsync(dogId, id);
            await LoadTreatments();
            Snackbar.Add("Traitement supprimé avec succès", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add("Erreur lors de la suppression du traitement", Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }
}