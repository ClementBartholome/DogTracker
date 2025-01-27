using DogTracker.Components;
using DogTracker.Data;
using DogTracker.Interfaces;
using DogTracker.Models;
using DogTracker.Services;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddScoped<IDogService, DogService>();
builder.Services.AddScoped<ILocationService, LocationService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    // seed data with a default dog
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
    var dogService = services.GetRequiredService<IDogService>();
    var dogs = await context.Dogs.ToListAsync(); 

    if (dogs.Count == 0)
    {
        var dog = new Dog
        {
            Name = "Mango",
            CurrentWeight = 22.3,
        };
        await dogService.AddDogAsync(dog);
        
        var weightRecord = new WeightRecord
        {
            Date = DateTime.UtcNow,
            Weight = 22.3,
            DogId = 1
        };
        await dogService.AddWeightRecordAsync(dog.Id, weightRecord);
    }
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();