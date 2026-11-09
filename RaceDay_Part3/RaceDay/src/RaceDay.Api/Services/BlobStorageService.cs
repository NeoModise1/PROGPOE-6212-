using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using RaceDay.Api.Settings;

namespace RaceDay.Api.Services;

public interface IBlobStorageService
{
    /// <summary>False when no storage connection string has been configured.</summary>
    bool IsConfigured { get; }

    /// <summary>Uploads an image into Azure Blob Storage and returns its public URL.</summary>
    Task<string> UploadAsync(string folder, string name, string originalFileName, string contentType, Stream content);

    /// <summary>Best-effort delete of a previously stored image (ignored if the URL is not ours).</summary>
    Task DeleteAsync(string? url);
}

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobStorageSettings _settings;
    private readonly ILogger<BlobStorageService> _logger;
    private BlobContainerClient? _container;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public BlobStorageService(IOptions<BlobStorageSettings> options, ILogger<BlobStorageService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.ConnectionString);

    private async Task<BlobContainerClient> GetContainerAsync()
    {
        if (_container is not null) return _container;

        await _lock.WaitAsync();
        try
        {
            if (_container is not null) return _container;

            var client = new BlobContainerClient(_settings.ConnectionString, _settings.ContainerName);
            // Images are public-read so the browser can show them directly from Blob Storage.
            await client.CreateIfNotExistsAsync(PublicAccessType.Blob);
            _container = client;
            return client;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string> UploadAsync(string folder, string name, string originalFileName, string contentType, Stream content)
    {
        var container = await GetContainerAsync();
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var blobName = $"{folder}/{name}-{Guid.NewGuid():N}{extension}";

        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        });

        return string.IsNullOrWhiteSpace(_settings.PublicBaseUrl)
            ? blob.Uri.ToString()
            : $"{_settings.PublicBaseUrl.TrimEnd('/')}/{blobName}";
    }

    public async Task DeleteAsync(string? url)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(url)) return;

        try
        {
            var container = await GetContainerAsync();
            var marker = $"/{_settings.ContainerName}/";
            var index = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return;

            var blobName = Uri.UnescapeDataString(url[(index + marker.Length)..]);
            await container.DeleteBlobIfExistsAsync(blobName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete old image {Url}", url);
        }
    }
}

/// <summary>Shared validation for uploaded images.</summary>
public static class ImageValidator
{
    public const long MaxBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    /// <summary>Returns an error message, or null when the file is acceptable.</summary>
    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0) return "Please choose an image file to upload.";
        if (file.Length > MaxBytes) return "The image must be 5 MB or smaller.";
        if (!AllowedTypes.Contains(file.ContentType.ToLowerInvariant())
            || !AllowedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
            return "Only JPEG, PNG, GIF or WebP images are allowed.";
        return null;
    }
}
