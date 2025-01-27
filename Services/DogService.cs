using DogTracker.Interfaces;
using DogTracker.Models;
using DogTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace DogTracker.Services;

public class DogService : IDogService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DogService> _logger;

    public DogService(AppDbContext context, ILogger<DogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Walk>> GetRecentWalksAsync(int dogId)
    {
        try
        {
            return await _context.Walks
                .Where(w => w.DogId == dogId)
                .OrderByDescending(w => w.StartTime)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des promenades pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible de récupérer l'historique des promenades.", ex);
        }
    }

    public async Task AddWalkAsync(int dogId, Walk? walk)
    {
        if (walk == null)
        {
            throw new ArgumentNullException(nameof(walk));
        }

        try
        {
            walk.DogId = dogId;
            walk.StartTime = walk.StartTime.ToUniversalTime();
            walk.EndTime = walk.EndTime.ToUniversalTime();
            
            _context.Walks.Add(walk);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Erreur lors de l'enregistrement de la promenade pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible d'enregistrer la promenade.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur inattendue lors de l'ajout de la promenade pour le chien {DogId}", dogId);
            throw new ApplicationException("Une erreur est survenue lors de l'enregistrement de la promenade.", ex);
        }
    }

    public async Task<List<WeightRecord>> GetWeightHistoryAsync(int dogId)
    {
        try
        {
            return await _context.WeightRecords
                .Where(w => w.DogId == dogId)
                .OrderByDescending(w => w.Date)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de l'historique du poids pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible de récupérer l'historique du poids.", ex);
        }
    }

    public async Task AddWeightRecordAsync(int dogId, WeightRecord weight)
    {
        if (weight == null)
        {
            throw new ArgumentNullException(nameof(weight));
        }

        try
        {
            weight.DogId = dogId;
            _context.WeightRecords.Add(weight);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Erreur lors de l'enregistrement du poids pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible d'enregistrer le poids.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur inattendue lors de l'ajout du poids pour le chien {DogId}", dogId);
            throw new ApplicationException("Une erreur est survenue lors de l'enregistrement du poids.", ex);
        }
    }

    public async Task<List<Expense>> GetExpensesAsync(int dogId, DateTime startDate, DateTime endDate)
    {
        try
        {
            startDate = startDate.ToUniversalTime();
            endDate = endDate.ToUniversalTime();
            return await _context.Expenses
                .Where(e => e.DogId == dogId && e.Date >= startDate && e.Date <= endDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des dépenses pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible de récupérer les dépenses.", ex);
        }
    }

    public async Task AddExpenseAsync(int dogId, Expense expense)
    {
        if (expense == null)
        {
            throw new ArgumentNullException(nameof(expense));
        }

        try
        {
            expense.DogId = dogId;
            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Erreur lors de l'enregistrement de la dépense pour le chien {DogId}", dogId);
            throw new ApplicationException("Impossible d'enregistrer la dépense.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur inattendue lors de l'ajout de la dépense pour le chien {DogId}", dogId);
            throw new ApplicationException("Une erreur est survenue lors de l'enregistrement de la dépense.", ex);
        }
    }
    
    public async Task AddDogAsync(Dog dog)
    {
        if (dog == null)
        {
            throw new ArgumentNullException(nameof(dog));
        }

        try
        {
            _context.Dogs.Add(dog);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Erreur lors de l'enregistrement du chien");
            throw new ApplicationException("Impossible d'enregistrer le chien.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur inattendue lors de l'ajout du chien");
            throw new ApplicationException("Une erreur est survenue lors de l'enregistrement du chien.", ex);
        }
    }
    
    public async Task<Walk?> GetWalkByIdAsync(int walkId)
    {
        try
        {
            return await _context.Walks.FindAsync(walkId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de la promenade {WalkId}", walkId);
            throw new ApplicationException("Impossible de récupérer la promenade.", ex);
        }
    }
}