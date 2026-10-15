using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

/// <summary>Event enrolments: Participants can enrol and the enrolment is correctly recorded.</summary>
public class EnrolmentTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EnrolmentTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Participant_CanEnrol_Returns201_WithCorrectLinks()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var eventId = await TestHelpers.CreateEvent(organiser);
        var catId = await TestHelpers.CreateCategory(organiser, eventId, "Senior");

        var res = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId = catId });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(eventId, json.GetProperty("eventId").GetInt32());
        Assert.Equal(catId, json.GetProperty("categoryId").GetInt32());
        Assert.Equal("Senior", json.GetProperty("categoryName").GetString());
    }

    [Fact]
    public async Task Enrolment_IsRecorded_AndVisibleToOrganiserAndParticipant()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var eventId = await TestHelpers.CreateEvent(organiser);
        var catId = await TestHelpers.CreateCategory(organiser, eventId);
        var enrolmentId = await TestHelpers.Enrol(participant, eventId, catId);

        var organiserView = await organiser.GetFromJsonAsync<JsonElement>($"/api/events/{eventId}/enrolments");
        Assert.Equal(1, organiserView.GetArrayLength());
        Assert.Equal(enrolmentId, organiserView[0].GetProperty("id").GetInt32());
        Assert.Equal(catId, organiserView[0].GetProperty("categoryId").GetInt32());

        var mine = await participant.GetFromJsonAsync<JsonElement>("/api/enrolments/mine");
        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(eventId, mine[0].GetProperty("eventId").GetInt32());
    }

    [Fact]
    public async Task Enrol_Twice_ForSameEvent_Returns409()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var eventId = await TestHelpers.CreateEvent(organiser);
        var catId = await TestHelpers.CreateCategory(organiser, eventId);
        await TestHelpers.Enrol(participant, eventId, catId);

        var second = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId = catId });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithCategoryFromAnotherEvent_Returns400()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var eventA = await TestHelpers.CreateEvent(organiser);
        var eventB = await TestHelpers.CreateEvent(organiser);
        var catOfB = await TestHelpers.CreateCategory(organiser, eventB);

        var res = await participant.PostAsJsonAsync($"/api/events/{eventA}/enrolments", new { categoryId = catOfB });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Enrol_InNonExistentEvent_Returns404()
    {
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var res = await participant.PostAsJsonAsync("/api/events/999999/enrolments", new { categoryId = 1 });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewEnrolmentsOfAnotherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var other = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEvent(owner);

        var res = await other.GetAsync($"/api/events/{eventId}/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Participant_OnlySeesOwnEnrolments()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var alice = await TestHelpers.LoggedInClient(_factory, "Participant");
        var bob = await TestHelpers.LoggedInClient(_factory, "Participant");
        var eventId = await TestHelpers.CreateEvent(organiser);
        var catId = await TestHelpers.CreateCategory(organiser, eventId);
        await TestHelpers.Enrol(alice, eventId, catId);

        var bobsList = await bob.GetFromJsonAsync<JsonElement>("/api/enrolments/mine");
        Assert.Equal(0, bobsList.GetArrayLength());
    }
}
