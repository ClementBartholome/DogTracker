using System.Text.Json;
using DogTracker.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Timer = System.Threading.Timer;

namespace DogTracker.Components.Pages
{
    public partial class WalkTracker : IDisposable, IAsyncDisposable
    {
        [Parameter] public int DogId { get; set; }

        private bool isTracking;
        private bool isLoading;
        private DateTime? startTime;
        private List<GeolocationPosition> positions = new();
        private double currentDistance = 0;
        private List<Walk?> walkHistory;
        private Timer timer;
        private IJSObjectReference? module;
        private IJSObjectReference? currentPositionMarker;

        
        [Inject] PersistentComponentState ApplicationState { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; } = null!;
        [Inject] private IDialogService DialogService { get; set; }

        private DateTime? _selectedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        
        protected override async Task OnInitializedAsync()
        {
            try
            {
                isLoading = true;
                walkHistory = await WalkService.GetWalksByMonthAsync(DogId, _selectedMonth?.Year ?? DateTime.Now.Year,
                    _selectedMonth?.Month ?? DateTime.Now.Month);

                
                // ApplicationState.RegisterOnPersisting(PersistState);
                var hasPersistentState = false;
                
                // if (ApplicationState.TryTakeFromJson<List<GeolocationPosition>>("positions", out var savedPositions) &&
                //     ApplicationState.TryTakeFromJson<double>("currentDistance", out var savedDistance) &&
                //     ApplicationState.TryTakeFromJson<DateTime?>("startTime", out var savedStartTime) &&
                //     ApplicationState.TryTakeFromJson<bool>("isTracking", out var savedIsTracking))
                // {
                //     positions = savedPositions;
                //     currentDistance = savedDistance;
                //     startTime = savedStartTime;
                //     isTracking = savedIsTracking;
                //     hasPersistentState = true;
                // }

                // Si pas de données persistantes, vérifions le localStorage
                if (!hasPersistentState)
                {
                    var ongoingWalk = await LocationService.CheckForOngoingWalkAsync();
                    if (ongoingWalk != null)
                    {
                        isTracking = true;
                        startTime = ongoingWalk.StartTime;
                        positions = ongoingWalk.Positions.ToList();

                        // Recalculer la distance totale
                        currentDistance = 0;
                        for (var i = 1; i < positions.Count; i++)
                        {
                            var prev = positions[i - 1];
                            var curr = positions[i];
                            currentDistance += CalculateDistance(
                                prev.Latitude, prev.Longitude,
                                curr.Latitude, curr.Longitude);
                        }

                        Snackbar.Add("Reprise de la promenade en cours", Severity.Info);
                    }
                }

                // Si on a une promenade en cours
                if (isTracking)
                {
                    await LocationService.StartWatchingPositionAsync();
                    timer = new Timer(_ => { InvokeAsync(StateHasChanged); }, null, 0, 1000);
                }

                // On n'enregistre l'événement qu'une seule fois
                LocationService.OnPositionChanged += OnPositionChanged;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'initialisation: {ex.Message}");
                Snackbar.Add("Erreur lors de l'initialisation", Severity.Error);
            }
            finally
            {
                isLoading = false;
            }
        }
        
        private async Task OnMonthChanged(DateTime? newMonth)
        {
            _selectedMonth = newMonth;
            if (_selectedMonth.HasValue)
            {
                var startDate = new DateTime(_selectedMonth.Value.Year, _selectedMonth.Value.Month, 1);
                walkHistory = await WalkService.GetWalksByMonthAsync(DogId, startDate.Year, startDate.Month);
            }
            else
            {
                walkHistory = await WalkService.GetRecentWalksAsync(DogId);
            }

            StateHasChanged();
        }

        private Task PersistState()
        {
            ApplicationState.PersistAsJson("positions", positions);
            ApplicationState.PersistAsJson("currentDistance", currentDistance);
            ApplicationState.PersistAsJson("startTime", startTime);
            ApplicationState.PersistAsJson("isTracking", isTracking);
            return Task.CompletedTask;
        }

        private async Task StartWalk()
        {
            try
            {
                isTracking = true;
                startTime = DateTime.Now;
                positions.Clear();
                currentDistance = 0;

                await LocationService.StartWatchingPositionAsync();

                Snackbar.Add("Promenade lancée !", Severity.Success);

                timer = new Timer(_ => { InvokeAsync(StateHasChanged); }, null, 0, 1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du démarrage de la promenade: {ex.Message}");
            }
        }

        private async Task ConfirmStopWalk()
        {
            var parameters = new DialogParameters
            {
                { "ContentText", "Es-tu sûr de vouloir terminer cette promenade ?" },
                { "ButtonText", "Terminer" },
                { "Color", Color.Error },
                { "ShowNotes", true }
            };

            var dialog = await DialogService.ShowAsync<Dialog.Dialog>("Fin de la promenade", parameters);
            var result = await dialog.Result;

            if (result is { Canceled: false, Data: string notes })
            {
                await StopWalk(notes);
            }
            else if (result is { Canceled: false })
            {
                await StopWalk(string.Empty);
            }
        }

        private async Task StopWalk(string notes)
        {
            if (!isTracking) return;

            try
            {
                isLoading = true;
                await LocationService.StopWatchingPositionAsync();
                timer?.Dispose();
                isTracking = false;

                var walk = new Walk
                {
                    StartTime = startTime!.Value,
                    EndTime = DateTime.Now,
                    Distance = currentDistance,
                    Route = JsonSerializer.Serialize(positions.Where((pos, index) => index % 100 == 0).ToList()),
                    Notes = notes
                };

                await WalkService.AddWalkAsync(DogId, walk);
                walkHistory = await WalkService.GetRecentWalksAsync(DogId);

                isLoading = false;
                Snackbar.Add("Promenade enregistrée !", Severity.Success);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'arrêt de la promenade: {ex.Message}");
                Snackbar.Add("Erreur lors de l'enregistrement de la promenade", Severity.Error);
            }
        }
        
        private async Task ConfirmDeleteWalk(int dogId, int walkId)
        {
            var parameters = new DialogParameters
            {
                { "ContentText", "Es-tu sûr de vouloir supprimer cette promenade ?" },
                { "ButtonText", "Supprimer" },
                { "Color", Color.Error }
            };

            var dialog = await DialogService.ShowAsync<Dialog.Dialog>("Suppression", parameters);
            var result = await dialog.Result;

            if (result is { Canceled: false })
            {
                await DeleteWalk(dogId, walkId);
            }
        }

        private async Task DeleteWalk(int dogId, int walkId)
        {
            try
            {
                isLoading = true;
                await WalkService.DeleteWalkAsync(dogId, walkId);
                walkHistory = await WalkService.GetRecentWalksAsync(dogId);
                Snackbar.Add("Promenade supprimée !", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add("Erreur lors de la suppression de la promenade", Severity.Error);
                Console.WriteLine($"Erreur lors de la suppression de la promenade: {ex.Message}");
            }
            finally
            {
                isLoading = false;
                StateHasChanged(); // Force le rafraîchissement
            }
        }

        private async void OnPositionChanged(object sender, GeolocationPosition position)
        {
            try
            {
                if (!isTracking) return;

                try
                {
                    if (positions.Count > 0)
                    {
                        var lastPos = positions.Last();
                        currentDistance += CalculateDistance(
                            lastPos.Latitude, lastPos.Longitude,
                            position.Latitude, position.Longitude);
                    }

                    positions.Add(position);
                    await UpdateCurrentPositionMarker(position.Latitude, position.Longitude);
                    await InvokeAsync(StateHasChanged);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la mise à jour de la position: {ex.Message}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erreur lors de la réception de la position: {e.Message}");
            }
        }

        private async Task UpdateCurrentPositionMarker(double lat, double lng)
        {
            try
            {
                if (currentPositionMarker == null)
                {
                    currentPositionMarker =
                        await JsRuntime.InvokeAsync<IJSObjectReference>("addCurrentPositionMarker", lat, lng);
                }
                else
                {
                    await currentPositionMarker.InvokeVoidAsync("setLatLng", lat, lng);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la mise à jour du marqueur de position: {ex.Message}");
            }
        }

        private string GetFormattedDuration()
        {
            if (!startTime.HasValue) return "00:00:00";
            var duration = DateTime.Now - startTime.Value.AddHours(2);
            return duration.ToString(@"hh\:mm\:ss");
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Rayon de la Terre en km
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRad(double degree)
        {
            return degree * Math.PI / 180;
        }

        public void Dispose()
        {
            LocationService.OnPositionChanged -= OnPositionChanged!;
            timer?.Dispose();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    await JsRuntime.InvokeVoidAsync("load_map");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors du chargement de la carte: {ex.Message}");
                }
            }
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (module != null)
            {
                try
                {
                    await module.DisposeAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la libération des ressources: {ex.Message}");
                }
            }
        }
    }
}