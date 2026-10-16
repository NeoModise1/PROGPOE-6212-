using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class ResultTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public ResultTests(ApiFactory factory) => _factory = factory;

    private static object FinishedBody(int position = 1) =>
        new { finishTime = "00:45:12", position, averagePace = 4.5, resultStatus = "Completed" };

    private async Task<(HttpClient organiser, HttpClient participant, int ecId, int enrollmentId)> SetupAsync()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var ecId = await TestHelpers.AttachCategoryAsync(organiser, eventId);
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var enrollmentId = await TestHelpers.EnrolAsync(participant, ecId);
        return (organiser, participant, ecId, enrollmentId);
    }

    [Fact]
    public async Task Capture_Organiser_Returns201()
    {
        var (organiser, _, _, enrollmentId) = await SetupAsync();
        var response = await organiser.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/results", FinishedBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await TestHelpers.ReadJsonAsync(response);
        Assert.Equal(1, json.GetProperty("position").GetInt32());
        Assert.Equal("Completed", json.GetProperty("resultStatus").GetString());
    }

    [Fact]
    public async Task Capture_Twice_Returns409()
    {
        var (organiser, _, _, enrollmentId) = await SetupAsync();
        await organiser.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/results", FinishedBody());

        var second = await organiser.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/results", FinishedBody(2));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Capture_Participant_Returns403()
    {
        var (_, participant, _, enrollmentId) = await SetupAsync();
        var response = await participant.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/results", FinishedBody());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Capture_OtherOrganiser_Returns403()
    {
        var (_, _, _, enrollmentId) = await SetupAsync();
        var other = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var response = await other.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/results", FinishedBody());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Capture_UnknownEnrollment_Returns404()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var response = await organiser.PostAsJsonAsync("/api/enrollments/999999/results", FinishedBody());
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Capture_CompletedWithoutTime_Returns400()
    {
        var (organiser, _, _, enrollmentId) = await SetupAsync();
        var response = await organiser.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/results",
            new { resultStatus = "Completed" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Capture_Dnf_WithoutTime_Returns201()
    {
        var (organiser, _, _, enrollmentId) = await SetupAsync();
        var response = await organiser.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/results",
            new { resultStatus = "DNF" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CategoryResults_Public_OrderedByPosition()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var ecId = await TestHelpers.AttachCategoryAsync(organiser, eventId);

        var p1 = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var p2 = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var e1 = await TestHelpers.EnrolAsync(p1, ecId);
        var e2 = await TestHelpers.EnrolAsync(p2, ecId);

        await organiser.PostAsJsonAsync($"/api/enrollments/{e1}/results", FinishedBody(2));
        await organiser.PostAsJsonAsync($"/api/enrollments/{e2}/results", FinishedBody(1));

        var response = await _factory.CreateClient().GetAsync($"/api/event-categories/{ecId}/results");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await TestHelpers.ReadJsonAsync(response);
        Assert.Equal(2, json.GetArrayLength());
        Assert.Equal(1, json[0].GetProperty("position").GetInt32());
        Assert.Equal(2, json[1].GetProperty("position").GetInt32());
    }

    [Fact]
    public async Task CategoryResults_UnknownCategory_Returns404()
    {
        var response = await _factory.CreateClient().GetAsync("/api/event-categories/999999/results");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MyResults_Participant_SeesOwnResults()
    {
        var (organiser, participant, _, enrollmentId) = await SetupAsync();
        await organiser.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/results", FinishedBody());

        var response = await participant.GetAsync("/api/users/me/results");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, (await TestHelpers.ReadJsonAsync(response)).GetArrayLength());
    }

    [Fact]
    public async Task MyResults_Organiser_Returns403()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var response = await organiser.GetAsync("/api/users/me/results");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
