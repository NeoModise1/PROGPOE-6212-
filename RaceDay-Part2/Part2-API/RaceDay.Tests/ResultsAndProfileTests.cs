using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

/// <summary>Results capture/viewing and profile management.</summary>
public class ResultsAndProfileTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ResultsAndProfileTests(RaceDayFactory factory) => _factory = factory;

    private async Task<(HttpClient organiser, HttpClient participant, int eventId, int enrolmentId)> Setup()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var participant = await TestHelpers.LoggedInClient(_factory, "Participant");
        var eventId = await TestHelpers.CreateEvent(organiser);
        var catId = await TestHelpers.CreateCategory(organiser, eventId);
        var enrolmentId = await TestHelpers.Enrol(participant, eventId, catId);
        return (organiser, participant, eventId, enrolmentId);
    }

    // ---------- Results ----------

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantCanViewIt()
    {
        var (organiser, participant, _, enrolmentId) = await Setup();

        var capture = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "01:45:30", position = 1 });
        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);

        var mine = await participant.GetFromJsonAsync<JsonElement>("/api/results/mine");
        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal("01:45:30", mine[0].GetProperty("finishTime").GetString());
        Assert.Equal(1, mine[0].GetProperty("position").GetInt32());
    }

    [Fact]
    public async Task Organiser_CanViewAllResultsForOwnEvent()
    {
        var (organiser, _, eventId, enrolmentId) = await Setup();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "00:50:00", position = 3 });

        var list = await organiser.GetFromJsonAsync<JsonElement>($"/api/events/{eventId}/results");
        Assert.Equal(1, list.GetArrayLength());
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var (organiser, _, _, enrolmentId) = await Setup();
        var body = new { finishTime = "00:40:00", position = 1 };
        (await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", body)).EnsureSuccessStatusCode();

        var second = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", body);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_DuplicatePositionInSameEvent_Returns409()
    {
        var (organiser, _, eventId, firstEnrolment) = await Setup();
        (await organiser.PostAsJsonAsync($"/api/enrolments/{firstEnrolment}/result",
            new { finishTime = "00:40:00", position = 1 })).EnsureSuccessStatusCode();

        var second = await TestHelpers.LoggedInClient(_factory, "Participant");
        var cats = await organiser.GetFromJsonAsync<JsonElement>($"/api/events/{eventId}/categories");
        var catId = cats[0].GetProperty("id").GetInt32();
        var secondEnrolment = await TestHelpers.Enrol(second, eventId, catId);

        var res = await organiser.PostAsJsonAsync($"/api/enrolments/{secondEnrolment}/result",
            new { finishTime = "00:42:00", position = 1 });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_InvalidPosition_Returns400()
    {
        var (organiser, _, _, enrolmentId) = await Setup();
        var res = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "00:40:00", position = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ZeroFinishTime_Returns400()
    {
        var (organiser, _, _, enrolmentId) = await Setup();
        var res = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "00:00:00", position = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForAnotherOrganisersEvent_Returns403()
    {
        var (_, _, _, enrolmentId) = await Setup();
        var other = await TestHelpers.LoggedInClient(_factory, "Organiser");

        var res = await other.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "00:40:00", position = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForUnknownEnrolment_Returns404()
    {
        var organiser = await TestHelpers.LoggedInClient(_factory, "Organiser");
        var res = await organiser.PostAsJsonAsync("/api/enrolments/999999/result",
            new { finishTime = "00:40:00", position = 1 });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCorrectResult()
    {
        var (organiser, participant, _, enrolmentId) = await Setup();
        (await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "00:40:00", position = 2 })).EnsureSuccessStatusCode();

        var update = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "00:39:15", position = 1 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var mine = await participant.GetFromJsonAsync<JsonElement>("/api/results/mine");
        Assert.Equal("00:39:15", mine[0].GetProperty("finishTime").GetString());
        Assert.Equal(1, mine[0].GetProperty("position").GetInt32());
    }

    [Fact]
    public async Task Participant_DoesNotSeeOtherParticipantsResults()
    {
        var (organiser, _, _, enrolmentId) = await Setup();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "00:40:00", position = 1 });

        var stranger = await TestHelpers.LoggedInClient(_factory, "Participant");
        var theirs = await stranger.GetFromJsonAsync<JsonElement>("/api/results/mine");
        Assert.Equal(0, theirs.GetArrayLength());
    }

    // ---------- Profile ----------

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task BothRoles_CanViewOwnProfile(string role)
    {
        var email = TestHelpers.UniqueEmail("profile");
        var client = await TestHelpers.LoggedInClient(_factory, role, email);

        var json = await client.GetFromJsonAsync<JsonElement>("/api/profile");
        Assert.Equal(email, json.GetProperty("email").GetString());
        Assert.Equal(role, json.GetProperty("role").GetString());
    }

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task BothRoles_CanUpdateOwnProfile(string role)
    {
        var client = await TestHelpers.LoggedInClient(_factory, role);

        var update = await client.PutAsJsonAsync("/api/profile",
            new { firstName = "Updated", lastName = "Name", phoneNumber = "0821234567" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var json = await client.GetFromJsonAsync<JsonElement>("/api/profile");
        Assert.Equal("Updated", json.GetProperty("firstName").GetString());
        Assert.Equal("0821234567", json.GetProperty("phoneNumber").GetString());
    }

    [Fact]
    public async Task UpdateProfile_MissingFirstName_Returns400()
    {
        var client = await TestHelpers.LoggedInClient(_factory, "Participant");
        var res = await client.PutAsJsonAsync("/api/profile", new { lastName = "Only" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
