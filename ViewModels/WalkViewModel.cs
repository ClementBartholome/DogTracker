namespace DogTracker.Models
{
    public class WalkViewModel
    {
        public Walk Walk { get; set; }

        public WalkViewModel(Walk walk)
        {
            Walk = walk;
        }

        public DateTime AdjustedStartTime => Walk.StartTime.AddHours(1);
        public DateTime AdjustedEndTime => Walk.EndTime.AddHours(1);
        public double Distance => Walk.Distance;
        public string? Route => Walk.Route;
        public string? Notes => Walk.Notes;
        public int DogId => Walk.DogId;
    }
}