using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Registration, login and logout. These are the only public endpoints.</summary>
[Route("api/auth")]
public class AuthController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(RaceDayDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>Register a new account as a Participant or an Organiser.</summary>
    /// <remarks>
    /// Participants can self-register. Organisers must supply the secret organiser code
    /// (configured in appsettings.json) so that anyone cannot make themselves an Organiser.
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var role = Roles.Normalise(request.Role);
        if (role is null)
            return BadRequestProblem("Role must be either 'Organiser' or 'Participant'.");

        if (role == Roles.Organiser)
        {
            var expected = _config["Registration:OrganiserCode"];
            if (string.IsNullOrWhiteSpace(expected) || request.OrganiserCode != expected)
                return Forbidden("A valid organiser code is required to register as an Organiser.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return ConflictProblem("An account with this email already exists.");

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = role,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, user.ToProfile());
    }

    /// <summary>Log in. Starts a session that stores the user's id and role.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            return Problem(detail: "Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized,
                title: "Login failed");

        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.Id);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);

        return Ok(user.ToProfile());
    }

    /// <summary>Log out and destroy the session.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return NoContent();
    }
}
