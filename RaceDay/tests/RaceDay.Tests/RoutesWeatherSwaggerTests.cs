using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class RoutesWeatherSwaggerTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public RoutesWeatherSwaggerTests(ApiFactory factory) => _factory = factory;

    private static object RouteBody(string name = "Main Loop") =>
        new { routeName = name, distanceKm = 10, elevationGain = 120, description = "Loop", mapUrl = "https://maps.example/loop" };

    [Fact]
    public async Task Routes_OrganiserAddUpdateDelete_AndPublicList()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);

        var add = await organiser.PostAsJsonAsync($"/api/events/{eventId}/routes", RouteBody());
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);
        var routeId = (await TestHelpers.ReadJsonAsync(add)).GetProperty("routeId").GetInt32();

        var list = await _factory.CreateClient().GetAsync($"/api/events/{eventId}/routes");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(1, (await TestHelpers.ReadJsonAsync(list)).GetArrayLength());

        var update = await organiser.PutAsJsonAsync($"/api/routes/{routeId}", RouteBody("Renamed Loop"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Renamed Loop", (await TestHelpers.ReadJsonAsync(update)).GetProperty("routeName").GetString());

        var delete = await organiser.DeleteAsync($"/api/routes/{routeId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Routes_Participant_Returns403()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/routes", RouteBody());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Routes_OtherOrganiser_Returns403()
    {
        var owner = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var other = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(owner);

        var response = await other.PostAsJsonAsync($"/api/events/{eventId}/routes", RouteBody());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Weather_OrganiserRecords_PublicReads()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);

        var post = await organiser.PostAsJsonAsync($"/api/events/{eventId}/weather",
            new { temperature = 22.5, weatherCondition = "Sunny", windSpeed = 12.3, humidity = 40 });
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var get = await _factory.CreateClient().GetAsync($"/api/events/{eventId}/weather");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(1, (await TestHelpers.ReadJsonAsync(get)).GetArrayLength());
    }

    [Fact]
    public async Task Weather_Participant_Returns403()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/weather",
            new { temperature = 20, weatherCondition = "Cloudy", windSpeed = 5, humidity = 50 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_Document_IsGeneratedAndListsEndpoints()
    {
        var response = await _factory.CreateClient().GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/auth/login", body);
        Assert.Contains("/api/events", body);
        Assert.Contains("/api/enrollments/{enrollmentId}/results", body);
    }
}
