using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : AppController
{
    public AccountController(IRaceDayApi api) : base(api) { }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (Me.IsAuthenticated) return RedirectHome();
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var login = await Api.SendAsync<LoginDto>(HttpMethod.Post, "api/auth/login",
            new { email = model.Email, password = model.Password });

        if (!login.Success || login.Data is null)
        {
            ModelState.AddModelError(string.Empty, login.IsUnauthorized
                ? "Incorrect email or password."
                : ErrorText(login));
            model.Password = string.Empty;
            return View(model);
        }

        // Keep the JWT and the user's role in the server-side session.
        HttpContext.Session.SignIn(login.Data.Token, login.Data.UserId, login.Data.Role, string.Empty, null);

        var me = await Api.GetAsync<UserDto>("api/users/me");
        if (me.Success && me.Data is not null)
        {
            HttpContext.Session.SetString(SessionKeys.Name, me.Data.FullName);
            HttpContext.Session.SetPicture(me.Data.ProfilePictureUrl);
        }

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectHome();
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (Me.IsAuthenticated) return RedirectHome();
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await Api.SendAsync(HttpMethod.Post, "api/auth/register", new
        {
            firstName = model.FirstName,
            lastName = model.LastName,
            email = model.Email,
            password = model.Password,
            role = model.Role,
            phoneNumber = model.PhoneNumber
        });

        if (!result.Success)
        {
            AddApiErrors(result, nameof(RegisterViewModel.Email));
            return View(model);
        }

        TempData["Success"] = "Your account has been created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        Response.Cookies.Delete(".RaceDay.Session");
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [SessionAuthorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var me = await Api.GetAsync<UserDto>("api/users/me");
        if (!me.Success || me.Data is null) return Failed(me);

        return View(ToViewModel(me.Data));
    }

    [SessionAuthorize]
    [HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(await WithReadOnlyFieldsAsync(model));

        var result = await Api.SendAsync<UserDto>(HttpMethod.Put, "api/users/me", new
        {
            firstName = model.FirstName,
            lastName = model.LastName,
            phoneNumber = model.PhoneNumber
        });

        if (!result.Success || result.Data is null)
        {
            if (result.IsUnauthorized) return Failed(result);
            AddApiErrors(result);
            return View(await WithReadOnlyFieldsAsync(model));
        }

        HttpContext.Session.SetString(SessionKeys.Name, result.Data.FullName);
        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    [SessionAuthorize]
    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture is null || picture.Length == 0)
        {
            TempData["Error"] = "Please choose an image to upload.";
            return RedirectToAction(nameof(Profile));
        }

        // The image goes to the API, which stores it in Azure Blob Storage.
        var result = await Api.UploadAsync<ImageUploadDto>("api/users/me/picture", picture);
        if (!result.Success || result.Data is null)
        {
            if (result.IsUnauthorized) return Failed(result);
            TempData["Error"] = ErrorText(result);
            return RedirectToAction(nameof(Profile));
        }

        HttpContext.Session.SetPicture(result.Data.Url);
        TempData["Success"] = "Your profile picture has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    /// <summary>Email, role and picture are not posted back by the form, so reload them when the page is shown again.</summary>
    private async Task<ProfileViewModel> WithReadOnlyFieldsAsync(ProfileViewModel model)
    {
        var me = await Api.GetAsync<UserDto>("api/users/me");
        if (me.Success && me.Data is not null)
        {
            model.Email = me.Data.Email;
            model.Role = me.Data.Role;
            model.ProfilePictureUrl = me.Data.ProfilePictureUrl;
        }
        return model;
    }

    private static ProfileViewModel ToViewModel(UserDto user) => new()
    {
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        Role = user.Role,
        ProfilePictureUrl = user.ProfilePictureUrl
    };
}
