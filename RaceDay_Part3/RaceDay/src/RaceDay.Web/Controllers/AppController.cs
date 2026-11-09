using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Shared helpers for all MVC controllers. Controllers only call the API - they never touch a database.</summary>
public abstract class AppController : Controller
{
    protected AppController(IRaceDayApi api)
    {
        Api = api;
    }

    protected IRaceDayApi Api { get; }

    protected CurrentUser Me => HttpContext.Session.GetCurrentUser();

    /// <summary>One line of user-safe text describing why an API call failed.</summary>
    protected static string ErrorText(ApiResult result) =>
        result.FieldErrors.Count > 0
            ? string.Join(" ", result.FieldErrors.Values.SelectMany(v => v))
            : result.Error ?? "Something went wrong. Please try again.";

    /// <summary>Copies API validation problems into ModelState so they show next to the form fields.</summary>
    protected void AddApiErrors(ApiResult result, string? conflictField = null)
    {
        foreach (var (field, messages) in result.FieldErrors)
            foreach (var message in messages)
                ModelState.AddModelError(field, message);

        if (result.FieldErrors.Count == 0 && result.Error is not null)
            ModelState.AddModelError(conflictField ?? string.Empty, result.Error);
        else if (result.FieldErrors.Count > 0 && result.Error is not null)
            ModelState.AddModelError(string.Empty, result.Error);
    }

    /// <summary>
    /// Handles a failed API call for a page or action that cannot continue: an expired session goes back to the
    /// login page, anything else shows a friendly message on the given page.
    /// </summary>
    protected IActionResult Failed(ApiResult result, string action = "Index", string controller = "Home", object? routeValues = null)
    {
        TempData["Error"] = result.IsUnauthorized ? result.Error : ErrorText(result);
        return result.IsUnauthorized
            ? RedirectToAction("Login", "Account")
            : RedirectToAction(action, controller, routeValues);
    }

    protected IActionResult RedirectHome()
    {
        var user = Me;
        return user.IsOrganiser
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Index", "Home");
    }
}
