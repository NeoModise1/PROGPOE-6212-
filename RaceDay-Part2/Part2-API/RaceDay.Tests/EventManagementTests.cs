using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

/// <summary>Event and category management by Organisers.</summary>
public class EventManagementTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventManagementTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var res = await organiser.PostAsJsonAsync("/api/events", TestHelpers.NewEvent("Soweto 10K"));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Soweto 10K", json.GetProperty("name").GetString());
        Assert.Equal("Run", json.GetProperty("eventType").GetString());
    }

    [Fact]
    public async Task CreateEvent_MissingName_Returns400()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var res = await organiser.PostAsJsonAsync("/api/events", new
        {
            description = "x", date = DateTime.UtcNow.AddDays(5), location = "JHB", distanceKm = 5, eventType = "Walk"
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_Returns400()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var res = await organiser.PostAsJsonAsync("/api/events", new
        {
            name = "Bad", description = "x", date = DateTime.UtcNow.AddDays(5), location = "JHB",
            distanceKm = 5, eventType = "Swim"
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var id = await TestHelpers.CreateEvent(organiser);

        var update = await organiser.PutAsJsonAsync($"/api/events/{id}", new
        {
            name = "Renamed Event", description = "new", date = DateTime.UtcNow.AddDays(60),
            location = "Cape Town", distanceKm = 21, eventType = "Cycle"
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var json = await (await organiser.GetAsync($"/api/events/{id}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Renamed Event", json.GetProperty("name").GetString());
        Assert.Equal("Cycle", json.GetProperty("eventType").GetString());
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var id = await TestHelpers.CreateEvent(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var get = await organiser.GetAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var other = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var id = await TestHelpers.CreateEvent(owner);

        var res = await other.PutAsJsonAsync($"/api/events/{id}", TestHelpers.NewEvent());
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnotherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var other = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var id = await TestHelpers.CreateEvent(owner);

        var res = await other.DeleteAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task BothRoles_CanViewEvents()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var id = await TestHelpers.CreateEvent(organiser);

        Assert.Equal(HttpStatusCode.OK, (await organiser.GetAsync("/api/events")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await participant.GetAsync("/api/events")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await participant.GetAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task GetEvent_NonExistent_Returns404()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var res = await organiser.GetAsync("/api/events/999999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ---------- Categories ----------

    [Fact]
    public async Task Organiser_CanCreateCategory_AndBothRolesCanView()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var eventId = await TestHelpers.CreateEvent(organiser);

        var create = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { name = "Under 20", minAge = 0, maxAge = 19 });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var list = await participant.GetFromJsonAsync<JsonElement>($"/api/events/{eventId}/categories");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("Under 20", list[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task CreateCategory_MinAgeGreaterThanMaxAge_Returns400()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEvent(organiser);

        var res = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { name = "Broken", minAge = 50, maxAge = 10 });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_Returns409()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEvent(organiser);
        await TestHelpers.CreateCategory(organiser, eventId, "10km");

        var res = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new { name = "10km" });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateAndDeleteCategory()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEvent(organiser);
        var catId = await TestHelpers.CreateCategory(organiser, eventId, "21km");

        var update = await organiser.PutAsJsonAsync($"/api/events/{eventId}/categories/{catId}",
            new { name = "Half Marathon", distanceKm = 21.1 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var delete = await organiser.DeleteAsync($"/api/events/{eventId}/categories/{catId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithEnrolments_Returns409()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var eventId = await TestHelpers.CreateEvent(organiser);
        var catId = await TestHelpers.CreateCategory(organiser, eventId);
        await TestHelpers.Enrol(participant, eventId, catId);

        var res = await organiser.DeleteAsync($"/api/events/{eventId}/categories/{catId}");
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }
}
