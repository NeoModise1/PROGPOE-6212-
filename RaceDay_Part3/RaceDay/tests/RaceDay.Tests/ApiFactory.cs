using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Tests;

/// <summary>Boots the real API in memory, swapping SQL Server for an isolated EF Core in-memory database.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"RaceDayTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<RaceDayDbContext>(o => o.UseInMemoryDatabase(_dbName));

            // Never talk to real Azure from tests.
            var blob = services.SingleOrDefault(d => d.ServiceType == typeof(IBlobStorageService));
            if (blob != null) services.Remove(blob);
            services.AddSingleton<IBlobStorageService, FakeBlobStorage>();
        });
    }
}

/// <summary>In-memory stand-in for Azure Blob Storage used by the tests.</summary>
public class FakeBlobStorage : IBlobStorageService
{
    public bool IsConfigured => true;

    public Task<string> UploadAsync(string folder, string name, string originalFileName, string contentType, Stream content) =>
        Task.FromResult($"https://fake.blob.test/raceday-images/{folder}/{name}-{Guid.NewGuid():N}{Path.GetExtension(originalFileName)}");

    public Task DeleteAsync(string? url) => Task.CompletedTask;
}
