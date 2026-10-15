using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

/// <summary>User registration and login (success and failure scenarios).</summary>
public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public AuthTests(RaceDayFactory factory) => _factory = factory;

    private static object RegisterBody(string email, string role = "Participant", string password = RaceDayFactory.Password,
        string? code = null) => new
    {
        firstName = "Jane",
        lastName = "Runner",
        email,
        password,
        role,
        organiserCode = code
    };

    [Fact]
    public async Task Register_Participant_Returns201()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register", RegisterBody(TestHelpers.UniqueEmail("p")));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithCorrectCode_Returns201()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register",
            RegisterBody(TestHelpers.UniqueEmail("o"), "Organiser", code: RaceDayFactory.OrganiserCode));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithWrongCode_Returns403()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register",
            RegisterBody(TestHelpers.UniqueEmail("o"), "Organiser", code: "WRONG"));
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithoutCode_Returns403()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register",
            RegisterBody(TestHelpers.UniqueEmail("o"), "Organiser"));
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.UniqueEmail("dup");
        (await client.PostAsJsonAsync("/api/auth/register", RegisterBody(email))).EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/auth/register", RegisterBody(email));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register", RegisterBody(TestHelpers.UniqueEmail("x"), "Admin"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register",
            RegisterBody(TestHelpers.UniqueEmail("x"), password: "short"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register", RegisterBody("not-an-email"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200_AndGrantsAccess()
    {
        var client = await TestHelpers.LoggedInClient(_factory, "Participant");
        var profile = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.UniqueEmail("p");
        (await client.PostAsJsonAsync("/api/auth/register", RegisterBody(email))).EnsureSuccessStatusCode();

        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUser_Returns401()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/login",
            new { email = TestHelpers.UniqueEmail("ghost"), password = RaceDayFactory.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_Email_IsCaseInsensitive()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.UniqueEmail("case");
        (await client.PostAsJsonAsync("/api/auth/register", RegisterBody(email))).EnsureSuccessStatusCode();

        var res = await client.PostAsJsonAsync("/api/auth/login",
            new { email = email.ToUpperInvariant(), password = RaceDayFactory.Password });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsSession()
    {
        var client = await TestHelpers.LoggedInClient(_factory, "Participant");
        (await client.PostAsync("/api/auth/logout", null)).EnsureSuccessStatusCode();

        var res = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Password_IsStoredHashed_NotPlainText()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.UniqueEmail("hash");
        (await client.PostAsJsonAsync("/api/auth/register", RegisterBody(email))).EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var user = db.Users.Single(u => u.Email == email);

        Assert.NotEqual(RaceDayFactory.Password, user.PasswordHash);
        Assert.DoesNotContain(RaceDayFactory.Password, user.PasswordHash);
    }
}
