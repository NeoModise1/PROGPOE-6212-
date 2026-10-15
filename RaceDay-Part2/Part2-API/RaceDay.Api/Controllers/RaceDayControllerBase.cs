using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;

namespace RaceDay.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class RaceDayControllerBase : ControllerBase
{
    /// <summary>Id of the logged-in user (from the session).</summary>
    protected int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;

    protected ObjectResult Forbidden(string detail) =>
        Problem(detail: detail, statusCode: StatusCodes.Status403Forbidden, title: "Access denied");

    protected ObjectResult ConflictProblem(string detail) =>
        Problem(detail: detail, statusCode: StatusCodes.Status409Conflict, title: "Conflict");

    protected ObjectResult BadRequestProblem(string detail) =>
        Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest, title: "Bad request");
}
