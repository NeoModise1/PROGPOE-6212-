using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : AppController
{
    public HomeController(IRaceDayApi api) : base(api) { }

    /// <summary>Upcoming events with filtering options (open to everyone).</summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? province, string? type, [FromQuery(Name = "from")] DateTime? fromDate)
    {
        var model = new HomeViewModel { Search = search, Province = province, EventType = type, From = fromDate };

        var result = await Api.GetAsync<List<EventDto>>("api/events");
        if (!result.Success || result.Data is null)
        {
            ViewData["Error"] = result.Error;
            return View(model);
        }

        var upcoming = result.Data.Where(e => e.IsUpcoming).OrderBy(e => e.EventDate).ToList();
        model.TotalUpcoming = upcoming.Count;
        model.Provinces = upcoming.Select(e => e.Province).Distinct().OrderBy(p => p).ToList();

        IEnumerable<EventDto> filtered = upcoming;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(e =>
                e.EventName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.City.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Venue.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (e.Description ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(province))
            filtered = filtered.Where(e => e.Province.Equals(province, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(type))
            filtered = filtered.Where(e => e.EventType.Equals(type, StringComparison.OrdinalIgnoreCase));
        if (fromDate.HasValue)
            filtered = filtered.Where(e => e.EventDate.Date >= fromDate.Value.Date);

        model.Events = filtered.ToList();
        return View(model);
    }

    /// <summary>Full event information with its categories, routes and weather.</summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var ev = await Api.GetAsync<EventDto>($"api/events/{id}");
        if (!ev.Success || ev.Data is null) return Failed(ev);

        var model = new EventDetailsViewModel { Event = ev.Data };

        var categories = await Api.GetAsync<List<EventCategoryDto>>($"api/events/{id}/categories");
        if (categories.Success && categories.Data is not null) model.Categories = categories.Data;

        var routes = await Api.GetAsync<List<RouteDto>>($"api/events/{id}/routes");
        if (routes.Success && routes.Data is not null) model.Routes = routes.Data;

        var weather = await Api.GetAsync<List<WeatherDto>>($"api/events/{id}/weather");
        if (weather.Success && weather.Data is not null) model.Weather = weather.Data;

        if (Me.IsParticipant)
        {
            var mine = await Api.GetAsync<List<EnrollmentDto>>("api/users/me/enrollments");
            if (mine.Success && mine.Data is not null)
                model.EnrolledCategoryIds = mine.Data.Select(e => e.EventCategoryId).ToHashSet();
        }

        return View(model);
    }

    // No verb restriction: the error pages are also re-executed for failed POST requests.
    [IgnoreAntiforgeryToken]
    public IActionResult Error(int? code)
    {
        ViewData["Code"] = code;
        Response.StatusCode = code ?? 500;
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
