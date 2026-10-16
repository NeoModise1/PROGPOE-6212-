using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

public class AuthController : ApiControllerBase
{
    private readonly IPasswordService _passwords;
    private readonly ITokenService _tokens;

    public AuthController(RaceDayDbContext db, IPasswordService passwords, ITokenService tokens) : base(db)
    {
        _passwords = passwords;
        _tokens = tokens;
    }

    /// <summary>Creates a new user account as either an Organiser or a Participant.</summary>
    /// <remarks>Role must be "Organiser" or "Participant". The password is hashed before it is stored.</remarks>
    /// <response code="201">Created - returns the new user's ID and role.</response>
    /// <response code="400">Bad Request - validation failed.</response>
    /// <response code="409">Conflict - email already registered.</response>
    [HttpPost("api/auth/register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var role = request.Role.Trim().ToLowerInvariant() switch
        {
            "organiser" => "Organiser",
            "participant" => "Participant",
            _ => null
        };
        if (role is null)
            return ValidationError(nameof(request.Role), "Role must be either 'Organiser' or 'Participant'.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await Db.Users.AnyAsync(u => u.Email == email))
            return ConflictProblem("A user with this email address is already registered.");

        var user = new AppUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PhoneNumber = request.PhoneNumber?.Trim(),
            PasswordHash = _passwords.Hash(request.Password),
            Role = role
        };

        if (role == "Organiser") user.Organiser = new Organiser();
        else user.Participant = new Participant();

        Db.Users.Add(user);
        await Db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new RegisterResponse(user.UserID, user.Role));
    }

    /// <summary>Authenticates a user and returns a JWT token for use on subsequent requests.</summary>
    /// <response code="200">OK - returns JWT token, UserID and Role.</response>
    /// <response code="401">Unauthorized - invalid credentials.</response>
    [HttpPost("api/auth/login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await Db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !_passwords.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new ProblemDetails { Title = "Invalid email or password.", Status = StatusCodes.Status401Unauthorized });

        return Ok(new LoginResponse(_tokens.CreateToken(user), user.UserID, user.Role));
    }
}
