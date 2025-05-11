using DogTracker.Components;
using DogTracker.Data;
using DogTracker.Interfaces;
using DogTracker.Jobs;
using DogTracker.Models;
using DogTracker.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Quartz;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.Configure<HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10 MB
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddMudServices();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsqlOptionsAction: options =>
        {
            options.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        }));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(Environment.GetEnvironmentVariable("POSTGRESQLCONNSTR_prod"), npgsqlOptionsAction: options =>
        {
            options.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        }));
}

builder.Services.AddHttpClient();

builder.Services.AddScoped<IDogService, DogService>();
builder.Services.AddScoped<IWalkService, WalkService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<ITreatmentService, TreatmentService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<IWeightService, WeightService>();
builder.Services.AddScoped<ContactService>();

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("ReminderJob", "NotificationGroup");
    
    q.AddJob<ReminderJob>(opts => opts.WithIdentity(jobKey));
    
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("ReminderTrigger", "NotificationGroup")
        .WithCronSchedule("0 55 11 * * ?"));  // Seconds, Minutes, Hours, Day of month, Month, Day of week
});

builder.Services.AddQuartzHostedService(opts => 
{
    opts.WaitForJobsToComplete = true;
});


            
builder.Services.AddHttpClient("OneSignalClient", client =>
{
    client.BaseAddress = new Uri("https://onesignal.com/"); 
    client.DefaultRequestHeaders.Add("Authorization", $"Basic {(builder.Environment.IsDevelopment() ? builder.Configuration["OneSignal:ApiKey:Test"] : Environment.GetEnvironmentVariable("APPSETTING_OneSignalApiKey"))}");
    client.Timeout = TimeSpan.FromSeconds(60);  
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("MesPromenades", policy =>
    {
        policy.WithOrigins(
                "https://walk-about-tracker.lovable.app",
                "https://localhost:7067",
                "http://localhost:3000",
                "exp://192.168.1.33:8081",  // URL de développement local
                "exp://exp.host/@clementbartholome/mes-promenades",  // URL de production
                "exp://u.expo.dev/update/8e4f36bf-7f4e-4c08-97b5-9817ef2b5dd1"  // URL du build internal distribution
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();

app.UseCors("MesPromenades");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => 
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = "swagger";
    });
    
    // seed data with a default dog
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
    var dogService = services.GetRequiredService<IDogService>();
    var weightService = services.GetRequiredService<IWeightService>();
    var dogs = await context.Dogs.ToListAsync(); 

    if (dogs.Count == 0)
    {
        var dog = new Dog
        {
            Name = "Mango",
        };
        await dogService.AddDogAsync(dog);
        
        var weightRecord = new WeightRecord
        {
            Date = DateTime.UtcNow,
            Weight = 22.3,
            DogId = 1
        };
        await weightService.AddWeightRecordAsync(dog.Id, weightRecord);
    }
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();

app.Run();