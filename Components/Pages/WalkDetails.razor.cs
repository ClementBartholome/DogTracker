using DogTracker.Models;
using DogTracker.Services;
using DogTracker.ViewModels;
using Microsoft.AspNetCore.Components;

namespace DogTracker.Components.Pages;

public partial class WalkDetails : ComponentBase
{
    [Parameter] public int WalkId { get; set; }

    private Walk? Walk { get; set; }
    private WalkViewModel? WalkViewModel { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Walk = await WalkService.GetWalkByIdAsync(WalkId);
        if (Walk != null) WalkViewModel = new WalkViewModel(Walk);
    }
}