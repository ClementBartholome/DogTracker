using DogTracker.Services;
using DogTracker.ViewModels;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DogTracker.Components.Pages
{
    public enum NotificationFilter
    {
        All,
        Read,
        Unread,
        Upcoming
    }

    public partial class Notifications : ComponentBase
    {
        [Inject] private NotificationService NotificationService { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!;
        [Inject] private IDialogService DialogService { get; set; } = null!;
        
        private bool isLoading = true;
        private List<NotificationViewModel> notifications = new();
        private MudChip<NotificationFilter> selectedFilter;
        private NotificationFilter currentFilter = NotificationFilter.All;
        
        protected override async Task OnInitializedAsync()
        {
            await LoadNotifications();
        }
        
        private IEnumerable<NotificationViewModel> filteredNotifications => currentFilter switch
        {
            NotificationFilter.Read => notifications.Where(n => n.IsDone),
            NotificationFilter.Unread => notifications.Where(n => !n.IsDone),
            NotificationFilter.Upcoming => notifications.Where(n => n.PlannedFor > DateTime.Now),
            _ => notifications
        };
        
        private async Task LoadNotifications()
        {
            isLoading = true;
            notifications = await NotificationService.GetAllNotifications();
            isLoading = false;
        }

        private async Task MarkNotificationAsDone(int notificationId)
        {
            try
            {
                await NotificationService.MarkNotificationAsDone(notificationId);
                await LoadNotifications();
                Snackbar.Add("Notification marquée comme lue !", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add("Erreur lors du marquage de la notification.", Severity.Error);
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task ConfirmDeleteNotification(int notificationId)
        {
            var parameters = new DialogParameters
            {
                { "ContentText", "Es-tu sûr de vouloir supprimer cette notification ?" },
                { "ButtonText", "Supprimer" },
                { "Color", Color.Error }
            };

            var dialog = await DialogService.ShowAsync<Dialog.Dialog>("Confirmation", parameters);
            var result = await dialog.Result;
            if (!result.Canceled)
            {
                await DeleteNotification(notificationId);
            }
        }

        private async Task DeleteNotification(int notificationId)
        {
            try
            {
                isLoading = true;
                await NotificationService.DeleteNotificationAsync(notificationId);
                await LoadNotifications();
                Snackbar.Add("Notification supprimée !", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add("Erreur lors de la suppression de la notification.", Severity.Error);
            }
            finally
            {
                isLoading = false;
            }
        }
    }
}