namespace RaceDay.Api.Settings;

public class BlobStorageSettings
{
    /// <summary>Azure Storage connection string (or "UseDevelopmentStorage=true" for Azurite). Empty = uploads disabled.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Blob container that holds all RaceDay images.</summary>
    public string ContainerName { get; set; } = "raceday-images";

    /// <summary>
    /// Optional. Base URL browsers use to reach the container (e.g. http://localhost:10000/devstoreaccount1/raceday-images
    /// when running Azurite in Docker). Leave empty on real Azure so the blob's own URL is used.
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}
