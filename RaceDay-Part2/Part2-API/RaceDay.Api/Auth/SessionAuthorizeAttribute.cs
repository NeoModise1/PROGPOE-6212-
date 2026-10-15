using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Auth;

public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
}

/// <summary>
/// Session-based authorisation.
/// No session  -> 401 Unauthorized.
/// Wrong role  -> 403 Forbidden.
/// Usage: [SessionAuthorize] (any logged-in user) or [SessionAuthorize(Roles.Organiser)].
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public SessionAuthorizeAttribute(params string[] roles) => _roles = roles;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (session.GetInt32(SessionKeys.UserId) is null)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication required",
                Detail = "You must log in before accessing this resource."
            })
            { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        var role = session.GetString(SessionKeys.Role);
        if (_roles.Length > 0 && !_roles.Contains(role))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Access denied",
                Detail = $"This action requires the role: {string.Join(" or ", _roles)}."
            })
            { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
