using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

/// <summary>Different roles / unauthenticated users are rejected from role-based functionality.</summary>
public class RoleAccessTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public RoleAccessTests(RaceDayFactory factory) => _factory = factory;

    // ---------- Unauthenticated ----------

    [Theory]
    [InlineData("/api/events")]
    [InlineData("/api/events/1")]
    [InlineData("/api/events/1/categories")]
    [InlineData("/api/profile")]
    [InlineData("/api/enrolments/mine")]
    [InlineData("/api/results/mine")]
    [InlineData("/api/events/1/enrolments")]
    [InlineData("/api/events/1/results")]
    public async Task Unauthenticated_GetProtectedEndpoint_Returns401(string url)
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_CreateEvent_Returns401()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/events", TestHelpers.NewEvent());
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_Enrol_Returns401()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/events/1/enrolments", new { categoryId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ---------- Participant attempting Organiser functionality ----------

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var res = await participant.PostAsJsonAsync("/api/events", TestHelpers.NewEvent());
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUpdateOrDeleteEvent_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var id = await TestHelpers.CreateEvent(organiser);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await participant.PutAsJsonAsync($"/api/events/{id}", TestHelpers.NewEvent())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await participant.DeleteAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateCategory_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var id = await TestHelpers.CreateEvent(organiser);

        var res = await participant.PostAsJsonAsync($"/api/events/{id}/categories", new { name = "Hack" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var id = await TestHelpers.CreateEvent(organiser);

        var res = await participant.GetAsync($"/api/events/{id}/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCaptureResult_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var eventId = await TestHelpers.CreateEvent(organiser);
        var catId = await TestHelpers.CreateCategory(organiser, eventId);
        var enrolmentId = await TestHelpers.Enrol(participant, eventId, catId);

        var res = await participant.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "00:45:00", position = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ---------- Organiser attempting Participant functionality ----------

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEvent(organiser);
        var catId = await TestHelpers.CreateCategory(organiser, eventId);

        var res = await organiser.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId = catId });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewParticipantOnlyEndpoints_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");

        Assert.Equal(HttpStatusCode.Forbidden, (await organiser.GetAsync("/api/enrolments/mine")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await organiser.GetAsync("/api/results/mine")).StatusCode);
    }
}
