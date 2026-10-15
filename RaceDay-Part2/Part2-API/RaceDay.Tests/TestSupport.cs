using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

/// <summary>Runs the real API in memory, with an in-memory database instead of SQL Server.</summary>
public class RaceDayFactory : WebApplicationFactory<Program>
{
    public const string OrganiserCode = "TEST-ORGANISER-CODE";
    public const string Password = "Passw0rd!";

    private readonly string _dbName = "RaceDayTests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Registration:OrganiserCode", OrganiserCode);

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<RaceDayDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}

public static class TestHelpers
{
    public static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@test.com";

    /// <summary>Creates a client (with its own cookie jar), registers a new user and logs them in.</summary>
    public static async Task<HttpClient> LoggedInClient(RaceDayFactory factory, string role, string? email = null)
    {
        var client = factory.CreateClient();
        email ??= UniqueEmail(role.ToLowerInvariant());

        var reg = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test",
            lastName = role,
            email,
            password = RaceDayFactory.Password,
            role,
            organiserCode = role == "Organiser" ? RaceDayFactory.OrganiserCode : null
        });
        reg.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = RaceDayFactory.Password });
        login.EnsureSuccessStatusCode();
        return client;
    }

    public static object NewEvent(string? name = null) => new
    {
        name = name ?? "Test Event " + Guid.NewGuid().ToString("N")[..6],
        description = "A test event",
        date = DateTime.UtcNow.AddDays(30),
        location = "Johannesburg",
        distanceKm = 10,
        eventType = "Run"
    };

    public static async Task<int> IdOf(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetInt32();
    }

    public static async Task<int> CreateEvent(HttpClient organiser)
    {
        var res = await organiser.PostAsJsonAsync("/api/events", NewEvent());
        res.EnsureSuccessStatusCode();
        return await IdOf(res);
    }

    public static async Task<int> CreateCategory(HttpClient organiser, int eventId, string name = "Senior")
    {
        var res = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { name, minAge = 20, maxAge = 120, distanceKm = 10 });
        res.EnsureSuccessStatusCode();
        return await IdOf(res);
    }

    public static async Task<int> Enrol(HttpClient participant, int eventId, int categoryId)
    {
        var res = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        res.EnsureSuccessStatusCode();
        return await IdOf(res);
    }
}
