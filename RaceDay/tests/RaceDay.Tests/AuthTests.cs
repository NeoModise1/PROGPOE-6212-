using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Services;

namespace RaceDay.Tests;

public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public AuthTests(ApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task Register_ValidUser_Returns201WithRole(string role)
    {
        var client = _factory.CreateClient();
        var response = await TestHelpers.RegisterAsync(client, TestHelpers.UniqueEmail("reg"), role);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await TestHelpers.ReadJsonAsync(response);
        Assert.Equal(role, json.GetProperty("role").GetString());
        Assert.True(json.GetProperty("userId").GetInt32() > 0);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.UniqueEmail("dup");
        await TestHelpers.RegisterAsync(client, email, "Participant");

        var second = await TestHelpers.RegisterAsync(client, email, "Organiser");
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = _factory.CreateClient();
        var response = await TestHelpers.RegisterAsync(client, TestHelpers.UniqueEmail("bad"), "Admin");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var client = _factory.CreateClient();
        var response = await TestHelpers.RegisterAsync(client, TestHelpers.UniqueEmail("pw"), "Participant", "short");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var client = _factory.CreateClient();
        var response = await TestHelpers.RegisterAsync(client, "not-an-email", "Participant");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenUserIdAndRole()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.UniqueEmail("login");
        await TestHelpers.RegisterAsync(client, email, "Organiser");

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestHelpers.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await TestHelpers.ReadJsonAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("token").GetString()));
        Assert.Equal("Organiser", json.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.UniqueEmail("wrong");
        await TestHelpers.RegisterAsync(client, email, "Participant");

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1!" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = TestHelpers.UniqueEmail("ghost"), password = TestHelpers.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void PasswordService_HashesAndVerifies()
    {
        var service = new PasswordService();
        var hash = service.Hash("Secret123!");

        Assert.NotEqual("Secret123!", hash);
        Assert.True(service.Verify("Secret123!", hash));
        Assert.False(service.Verify("Other123!", hash));
        Assert.NotEqual(hash, service.Hash("Secret123!")); // random salt
    }
}
