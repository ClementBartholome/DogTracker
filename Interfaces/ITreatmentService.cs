using DogTracker.Models;

namespace DogTracker.Interfaces;

public interface ITreatmentService
{
    Task<List<Treatment>> GetTreatmentsAsync(int dogId, int year, int month);
    Task<Treatment> GetLastTreatment(int dogId);
    Task AddTreatmentAsync(int dogId, Treatment treatment);
    Task DeleteTreatmentAsync(int dogId, int treatmentId);
}