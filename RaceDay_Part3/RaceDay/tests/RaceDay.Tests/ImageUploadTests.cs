using System.Net;
using System.Net.Http.Headers;

namespace RaceDay.Tests;

public class ImageUploadTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public ImageUploadTests(ApiFactory factory) => _factory = factory;

    private static MultipartFormDataContent ImageContent(string fileName = "banner.png", string contentType = "image/png", int size = 64)
    {
        var file = new ByteArrayContent(new byte[size]);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    [Fact]
    public async Task UploadBanner_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().PostAsync("/api/events/1/banner", ImageContent());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_AsParticipant_Returns403()
    {
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var response = await participant.PostAsync("/api/events/1/banner", ImageContent());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_OwnerValidImage_ReturnsUrlAndSavesItOnTheEvent()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);

        var response = await organiser.PostAsync($"/api/events/{eventId}/banner", ImageContent());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var url = (await TestHelpers.ReadJsonAsync(response)).GetProperty("url").GetString();
        Assert.False(string.IsNullOrWhiteSpace(url));

        var ev = await TestHelpers.ReadJsonAsync(await organiser.GetAsync($"/api/events/{eventId}"));
        Assert.Equal(url, ev.GetProperty("bannerUrl").GetString());
    }

    [Fact]
    public async Task UploadBanner_NotTheOwner_Returns403()
    {
        var owner = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var other = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(owner);

        var response = await other.PostAsync($"/api/events/{eventId}/banner", ImageContent());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_NonImageFile_Returns400()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);

        var response = await organiser.PostAsync($"/api/events/{eventId}/banner", ImageContent("notes.txt", "text/plain"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_UnknownEvent_Returns404()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var response = await organiser.PostAsync("/api/events/999999/banner", ImageContent());
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().PostAsync("/api/users/me/picture", ImageContent());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_ValidImage_AppearsOnProfile()
    {
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");

        var response = await participant.PostAsync("/api/users/me/picture", ImageContent("me.jpg", "image/jpeg"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var url = (await TestHelpers.ReadJsonAsync(response)).GetProperty("url").GetString();

        var me = await TestHelpers.ReadJsonAsync(await participant.GetAsync("/api/users/me"));
        Assert.Equal(url, me.GetProperty("profilePictureUrl").GetString());
    }

    [Fact]
    public async Task UploadProfilePicture_MissingFile_Returns400()
    {
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var response = await participant.PostAsync("/api/users/me/picture", new MultipartFormDataContent());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
