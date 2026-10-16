using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class ProfileTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public ProfileTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetMe_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_InvalidToken_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not.a.token");
        var response = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_Authenticated_ReturnsProfileWithoutPasswordHash()
    {
        var client = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await TestHelpers.ReadJsonAsync(response);
        Assert.Equal("Participant", json.GetProperty("role").GetString());
        Assert.False(json.TryGetProperty("passwordHash", out _));
    }

    [Fact]
    public async Task UpdateMe_ValidBody_ReturnsUpdatedProfile()
    {
        var client = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var response = await client.PutAsJsonAsync("/api/users/me",
            new { firstName = "Thabo", lastName = "Mokoena", phoneNumber = "0831112222" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await TestHelpers.ReadJsonAsync(response);
        Assert.Equal("Thabo", json.GetProperty("firstName").GetString());
        Assert.Equal("0831112222", json.GetProperty("phoneNumber").GetString());
    }

    [Fact]
    public async Task UpdateMe_MissingFirstName_Returns400()
    {
        var client = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var response = await client.PutAsJsonAsync("/api/users/me",
            new { firstName = "", lastName = "Mokoena", phoneNumber = "0831112222" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
