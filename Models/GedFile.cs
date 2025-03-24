using DogTracker.Enums;

namespace DogTracker.Models;

public class GedFile
{
    public string PreviewUrl { get; set; }
    public string DownloadUrl { get; set; }
    public TypeFilesEnum Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Name { get; set; }
}