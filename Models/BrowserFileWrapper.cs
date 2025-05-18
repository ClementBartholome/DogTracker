using Microsoft.AspNetCore.Components.Forms;

namespace DogTracker.Models;

public class BrowserFileWrapper(Stream stream, string name) : IBrowserFile
{
    public string Name { get; } = name;
    public DateTimeOffset LastModified => DateTimeOffset.Now;
    public long Size => stream.Length;
    public string ContentType => "application/octet-stream";

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        if (stream.Length > maxAllowedSize)
        {
            throw new IOException("Le fichier dépasse la taille maximale autorisée.");
        }
        return stream;
    }
}