using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

public static class TestHelpers
{
    public const string Password = "Password123!";

    public static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@raceday.test";

    public static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string role, string password = Password) =>
        client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test",
            lastName = role,
            email,
            password,
            role,
            phoneNumber = "0821234567"
        });

    public static async Task<string> LoginAsync(HttpClient client, string email, string password = Password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var json = await ReadJsonAsync(response);
        return json.GetProperty("token").GetString()!;
    }

    /// <summary>Registers a new user with the given role, logs in and returns a client carrying the JWT.</summary>
    public static async Task<HttpClient> CreateUserClientAsync(ApiFactory factory, string role)
    {
        var client = factory.CreateClient();
        var email = UniqueEmail(role.ToLowerInvariant());
        var register = await RegisterAsync(client, email, role);
        register.EnsureSuccessStatusCode();
        var token = await LoginAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    public static object NewEventBody(string name = "Soweto Fun Run") => new
    {
        eventName = name,
        description = "A friendly community race",
        eventDate = "2027-03-15",
        startTime = "06:30:00",
        venue = "FNB Stadium",
        city = "Johannesburg",
        province = "Gauteng"
    };

    public static async Task<int> CreateEventAsync(HttpClient organiser, string name = "Soweto Fun Run")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", NewEventBody(name));
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("eventId").GetInt32();
    }

    public static async Task<int> GetFirstCategoryIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/categories");
        response.EnsureSuccessStatusCode();
        var json = await ReadJsonAsync(response);
        return json[0].GetProperty("categoryId").GetInt32();
    }

    public static async Task<int> AttachCategoryAsync(HttpClient organiser, int eventId, int max = 100)
    {
        var categoryId = await GetFirstCategoryIdAsync(organiser);
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryId, entryFee = 150.00m, maximumParticipants = max });
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("eventCategoryId").GetInt32();
    }

    public static async Task<int> EnrolAsync(HttpClient participant, int eventCategoryId)
    {
        var response = await participant.PostAsync($"/api/event-categories/{eventCategoryId}/enrollments", null);
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("enrollmentId").GetInt32();
    }
}
