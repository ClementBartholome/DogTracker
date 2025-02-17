using System.Text.Json.Serialization;

namespace DogTracker.Models;

public class OneSignalResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}