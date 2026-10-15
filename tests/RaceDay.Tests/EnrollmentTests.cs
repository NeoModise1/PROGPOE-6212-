using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class EnrollmentTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public EnrollmentTests(ApiFactory factory) => _factory = factory;

    private async Task<(HttpClient organiser, int eventId, int ecId)> SetupEventAsync(int max = 100)
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var ecId = await TestHelpers.AttachCategoryAsync(organiser, eventId, max);
        return (organiser, eventId, ecId);
    }

    [Fact]
    public async Task Enrol_Participant_Returns201AndIsRecorded()
    {
        var (_, _, ecId) = await SetupEventAsync();
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");

        var response = await participant.PostAsync($"/api/event-categories/{ecId}/enrollments", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await TestHelpers.ReadJsonAsync(response);
        Assert.Equal("Enrolled", json.GetProperty("status").GetString());

        var mine = await participant.GetAsync("/api/users/me/enrollments");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        Assert.Equal(1, (await TestHelpers.ReadJsonAsync(mine)).GetArrayLength());
    }

    [Fact]
    public async Task Enrol_CreatesEventRegistration()
    {
        var (_, _, ecId) = await SetupEventAsync();
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        await TestHelpers.EnrolAsync(participant, ecId);

        var events = await participant.GetAsync("/api/users/me/events");
        Assert.Equal(HttpStatusCode.OK, events.StatusCode);
        Assert.Equal(1, (await TestHelpers.ReadJsonAsync(events)).GetArrayLength());
    }

    [Fact]
    public async Task Enrol_Organiser_Returns403()
    {
        var (organiser, _, ecId) = await SetupEventAsync();
        var response = await organiser.PostAsync($"/api/event-categories/{ecId}/enrollments", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_Unauthenticated_Returns401()
    {
        var (_, _, ecId) = await SetupEventAsync();
        var response = await _factory.CreateClient().PostAsync($"/api/event-categories/{ecId}/enrollments", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_UnknownCategory_Returns404()
    {
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var response = await participant.PostAsync("/api/event-categories/999999/enrollments", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_Twice_Returns409()
    {
        var (_, _, ecId) = await SetupEventAsync();
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        await TestHelpers.EnrolAsync(participant, ecId);

        var second = await participant.PostAsync($"/api/event-categories/{ecId}/enrollments", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenCategoryFull_Returns409()
    {
        var (_, _, ecId) = await SetupEventAsync(max: 1);
        var first = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var second = await TestHelpers.CreateUserClientAsync(_factory, "Participant");

        await TestHelpers.EnrolAsync(first, ecId);
        var response = await second.PostAsync($"/api/event-categories/{ecId}/enrollments", null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetEventEnrollments_OwnerSeesEnrolments()
    {
        var (organiser, eventId, ecId) = await SetupEventAsync();
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        await TestHelpers.EnrolAsync(participant, ecId);

        var response = await organiser.GetAsync($"/api/events/{eventId}/enrollments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, (await TestHelpers.ReadJsonAsync(response)).GetArrayLength());
    }

    [Fact]
    public async Task GetEventEnrollments_OtherOrganiser_Returns403()
    {
        var (_, eventId, _) = await SetupEventAsync();
        var other = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var response = await other.GetAsync($"/api/events/{eventId}/enrollments");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEventEnrollments_Participant_Returns403()
    {
        var (_, eventId, _) = await SetupEventAsync();
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var response = await participant.GetAsync($"/api/events/{eventId}/enrollments");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MyEnrollments_Organiser_Returns403()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var response = await organiser.GetAsync("/api/users/me/enrollments");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_OwnEnrollment_Returns204AndFreesSpot()
    {
        var (_, _, ecId) = await SetupEventAsync(max: 1);
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var enrollmentId = await TestHelpers.EnrolAsync(participant, ecId);

        var cancel = await participant.DeleteAsync($"/api/enrollments/{enrollmentId}");
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var other = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var enrol = await other.PostAsync($"/api/event-categories/{ecId}/enrollments", null);
        Assert.Equal(HttpStatusCode.Created, enrol.StatusCode);
    }

    [Fact]
    public async Task Cancel_SomeoneElsesEnrollment_Returns403()
    {
        var (_, _, ecId) = await SetupEventAsync();
        var owner = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var other = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var enrollmentId = await TestHelpers.EnrolAsync(owner, ecId);

        var response = await other.DeleteAsync($"/api/enrollments/{enrollmentId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_UnknownEnrollment_Returns404()
    {
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var response = await participant.DeleteAsync("/api/enrollments/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Registrations_OrganiserAssignsRaceNumber()
    {
        var (organiser, eventId, ecId) = await SetupEventAsync();
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        await TestHelpers.EnrolAsync(participant, ecId);

        var list = await organiser.GetAsync($"/api/events/{eventId}/registrations");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var registrationId = (await TestHelpers.ReadJsonAsync(list))[0].GetProperty("userEventId").GetInt32();

        var update = await organiser.PutAsJsonAsync($"/api/user-events/{registrationId}",
            new { raceNumber = "A101", status = "Confirmed" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var json = await TestHelpers.ReadJsonAsync(update);
        Assert.Equal("A101", json.GetProperty("raceNumber").GetString());
        Assert.Equal("Confirmed", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Registrations_ParticipantCannotUpdate_Returns403()
    {
        var (organiser, eventId, ecId) = await SetupEventAsync();
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        await TestHelpers.EnrolAsync(participant, ecId);
        var list = await organiser.GetAsync($"/api/events/{eventId}/registrations");
        var registrationId = (await TestHelpers.ReadJsonAsync(list))[0].GetProperty("userEventId").GetInt32();

        var update = await participant.PutAsJsonAsync($"/api/user-events/{registrationId}",
            new { raceNumber = "Z999", status = "Confirmed" });
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
    }
}
