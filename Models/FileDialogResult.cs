using DogTracker.Enums;

namespace DogTracker.Models;

public class FileDialogResult
{
    public TypeFilesEnum Category { get; set; }
    public string Name { get; set; }
}