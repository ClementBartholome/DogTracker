using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Components.Forms;

namespace DogTracker.Services;

public class BlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production" 
            ? Environment.GetEnvironmentVariable("AzureStorage") 
            : configuration["ConnectionStrings:AzureStorage"];
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("La variable d'environnement 'AzureStorage' n'est pas définie.");
        }

        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadFileAsync(IBrowserFile file, string containerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobName = $"{Guid.NewGuid()}-{file.Name}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream(maxAllowedSize: 10485760); // 10MB max
        await blobClient.UploadAsync(stream, overwrite: true);

        return blobClient.Uri.ToString();
    }
    
    public async Task DeleteFileAsync(string fileUrl)
    {
        var uri = new Uri(fileUrl);
        var blobName = uri.Segments.Last();
        var containerName = uri.Segments[1].TrimEnd('/');

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.DeleteIfExistsAsync();
    }
    
    public BlobContainerClient GetContainerClient(string containerName)
    {
        return _blobServiceClient.GetBlobContainerClient(containerName);
    }
}