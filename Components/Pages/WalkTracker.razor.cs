using System.Text.Json;
using DogTracker.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Timer = System.Threading.Timer;

namespace DogTracker.Components.Pages
{
    public partial class WalkTracker : IDisposable, IAsyncDisposable
    {
        [Parameter] public int DogId { get; set; }

        private bool isTracking = false;
        private bool isLoading = true;
        private DateTime? startTime;
        private List<GeolocationPosition> positions = new();
        private double currentDistance = 0;
        private List<Walk> walkHistory;
        private Timer timer;
        private IJSObjectReference? module;
        private IJSObjectReference? currentPositionMarker;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                isLoading = true;
                walkHistory = await DogService.GetRecentWalksAsync(DogId);
                LocationService.OnPositionChanged += OnPositionChanged;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'initialisation: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
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

                timer = new Timer(_ => { InvokeAsync(StateHasChanged); }, null, 0, 1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du démarrage de la promenade: {ex.Message}");
            }
        }

        private async Task StopWalk()
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
                    StartTime = startTime.Value,
                    EndTime = DateTime.Now,
                    Distance = currentDistance,
                    Route = JsonSerializer.Serialize(positions)
                };

                await DogService.AddWalkAsync(DogId, walk);
                walkHistory = await DogService.GetRecentWalksAsync(DogId);
                isLoading = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'arrêt de la promenade: {ex.Message}");
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
                    currentPositionMarker = await JSRuntime.InvokeAsync<IJSObjectReference>("addCurrentPositionMarker", lat, lng);
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
            var duration = DateTime.Now - startTime.Value;
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

        private double ToRad(double degree)
        {
            return degree * Math.PI / 180;
        }

        public void Dispose()
        {
            LocationService.OnPositionChanged -= OnPositionChanged;
            timer?.Dispose();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("load_map");
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