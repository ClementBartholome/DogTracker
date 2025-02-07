using DogTracker.Models;

namespace DogTracker.Interfaces
{
    public interface IWalkService
    {
        Task<List<Walk?>> GetRecentWalksAsync(int dogId);
        Task AddWalkAsync(int dogId, Walk? walk);
        Task DeleteWalkAsync(int dogId, int walkId);
        Task<Walk?> GetWalkByIdAsync(int walkId);
        Task<List<Walk?>> GetWalksByMonthAsync(int dogId, int year, int month);
    }
}