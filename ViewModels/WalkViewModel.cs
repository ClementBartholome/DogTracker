using DogTracker.Models;

namespace DogTracker.ViewModels
{
    public class WalkViewModel
    {
        private Walk Walk { get; set; }

        public WalkViewModel(Walk walk)
        {
            Walk = walk;
        }

        public int Id => Walk.Id;
        public DateTime AdjustedStartTime => Walk.StartTime.AddHours(1);
        public DateTime AdjustedEndTime => Walk.EndTime.AddHours(1);
        public double Distance => Walk.Distance;
        public string? Notes => Walk.Notes;
    }
}