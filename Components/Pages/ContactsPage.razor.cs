using DogTracker.Components.Dialog;
using DogTracker.Models;
using DogTracker.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DogTracker.Components.Pages;

public partial class ContactsPage
{
    [Inject] private ContactService ContactService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    private bool isLoading = true;
    private List<Contact>? contacts;
    [Parameter] public int dogId { get; set; } = 1;

    protected override async Task OnInitializedAsync()
    {
        await LoadContacts();
    }

    private async Task LoadContacts()
    {
        isLoading = true;
        try
        {
            contacts = await ContactService.GetContactsAsync(dogId);
        }
        catch
        {
            Snackbar.Add("Erreur lors du chargement des contacts", Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OpenAddContactDialog()
    {
        var parameters = new DialogParameters();
        var dialog = DialogService.Show<AddEditContactDialog>("Ajouter un contact", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false, Data: Contact contact })
        {
            await AddContact(contact);
        }
    }

    private async Task OpenEditContactDialog(Contact contact)
    {
        var parameters = new DialogParameters { { "Contact", contact } };
        var dialog = DialogService.Show<AddEditContactDialog>("Modifier le contact", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false, Data: Contact updatedContact })
        {
            await UpdateContact(updatedContact);
        }
    }

    private async Task AddContact(Contact contact)
    {
        isLoading = true;
        try
        {
            await ContactService.AddContactAsync(dogId, contact);
            await LoadContacts();
            Snackbar.Add("Contact ajouté avec succès", Severity.Success);
        }
        catch
        {
            Snackbar.Add("Erreur lors de l'ajout du contact", Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task UpdateContact(Contact contact)
    {
        isLoading = true;
        try
        {
            await ContactService.UpdateContactAsync(contact);
            await LoadContacts();
            Snackbar.Add("Contact modifié avec succès", Severity.Success);
        }
        catch
        {
            Snackbar.Add("Erreur lors de la modification du contact", Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task ConfirmDeleteContact(int contactId)
    {
        var parameters = new DialogParameters
        {
            { "ContentText", "Es-tu sûr de vouloir supprimer ce contact ?" },
            { "ButtonText", "Supprimer" },
            { "Color", Color.Error }
        };

        var dialog = await DialogService.ShowAsync<Dialog.Dialog>("Confirmation", parameters);
        var result = await dialog.Result;
        if (!result.Canceled)
        {
            await DeleteContact(contactId);
        }
    }

    private async Task DeleteContact(int id)
    {
        isLoading = true;
        try
        {
            await ContactService.DeleteContactAsync(id);
            await LoadContacts();
            Snackbar.Add("Contact supprimé avec succès", Severity.Success);
        }
        catch
        {
            Snackbar.Add("Erreur lors de la suppression du contact", Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }
}