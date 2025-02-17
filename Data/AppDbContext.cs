using DogTracker.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DogTracker.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Dog> Dogs { get; set; }
    public DbSet<Walk?> Walks { get; set; }
    public DbSet<WeightRecord> WeightRecords { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<Treatment> Treatments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
}