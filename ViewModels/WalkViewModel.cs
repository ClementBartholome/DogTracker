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
        public DateTime AdjustedStartTime => TimeZoneInfo.ConvertTime(Walk.StartTime, TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"));
        public DateTime AdjustedEndTime => TimeZoneInfo.ConvertTime(Walk.EndTime, TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"));
        public double Distance => Walk.Distance;
        public string? Notes => Walk.Notes;
    }
}