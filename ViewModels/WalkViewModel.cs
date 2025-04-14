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
        public DateTime AdjustedStartTime => Walk.StartTime.ToLocalTime();
        public DateTime AdjustedEndTime => Walk.EndTime.ToLocalTime();
        public double Distance => Walk.Distance;
        public string? Notes => Walk.Notes;
    }
}