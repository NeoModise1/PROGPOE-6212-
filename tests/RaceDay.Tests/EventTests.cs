using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class EventTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public EventTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetEvents_Public_Returns200()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_Organiser_Returns201AndEventIsRetrievable()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var response = await organiser.PostAsJsonAsync("/api/events", TestHelpers.NewEventBody("Joburg 10K"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await TestHelpers.ReadJsonAsync(response)).GetProperty("eventId").GetInt32();

        var get = await _factory.CreateClient().GetAsync($"/api/events/{id}"); // public
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("Joburg 10K", (await TestHelpers.ReadJsonAsync(get)).GetProperty("eventName").GetString());
    }

    [Fact]
    public async Task CreateEvent_Participant_Returns403()
    {
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var response = await participant.PostAsJsonAsync("/api/events", TestHelpers.NewEventBody());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/events", TestHelpers.NewEventBody());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_MissingName_Returns400()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventName = "",
            eventDate = "2027-03-15",
            startTime = "06:30:00",
            venue = "V",
            city = "C",
            province = "P"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_UnknownId_Returns404()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_Owner_Returns200()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(organiser);

        var response = await organiser.PutAsJsonAsync($"/api/events/{id}", new
        {
            eventName = "Renamed Race",
            description = "Updated",
            eventDate = "2027-04-01",
            startTime = "07:00:00",
            venue = "New Venue",
            city = "Pretoria",
            province = "Gauteng",
            status = "Ongoing"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await TestHelpers.ReadJsonAsync(response);
        Assert.Equal("Renamed Race", json.GetProperty("eventName").GetString());
        Assert.Equal("Ongoing", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task UpdateEvent_DifferentOrganiser_Returns403()
    {
        var owner = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var other = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(owner);

        var response = await other.PutAsJsonAsync($"/api/events/{id}", new
        {
            eventName = "Hijack",
            eventDate = "2027-04-01",
            startTime = "07:00:00",
            venue = "V",
            city = "C",
            province = "P",
            status = "Upcoming"
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_InvalidStatus_Returns400()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(organiser);

        var response = await organiser.PutAsJsonAsync($"/api/events/{id}", new
        {
            eventName = "X",
            eventDate = "2027-04-01",
            startTime = "07:00:00",
            venue = "V",
            city = "C",
            province = "P",
            status = "Banana"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_UnknownId_Returns404()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var response = await organiser.PutAsJsonAsync("/api/events/999999", new
        {
            eventName = "X",
            eventDate = "2027-04-01",
            startTime = "07:00:00",
            venue = "V",
            city = "C",
            province = "P",
            status = "Upcoming"
        });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_Owner_Returns204ThenNotFound()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var get = await organiser.GetAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_DifferentOrganiser_Returns403()
    {
        var owner = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var other = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(owner);

        var response = await other.DeleteAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_Participant_Returns403()
    {
        var owner = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var id = await TestHelpers.CreateEventAsync(owner);

        var response = await participant.DeleteAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
