using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(Roles = "Organiser")]
public class OrganiserController : AppController
{
    public OrganiserController(IRaceDayApi api) : base(api) { }

    // ---------------------------------------------------------------- helpers

    private Task<ApiResult<List<EventDto>>> LoadMyEventsAsync() =>
        Api.GetAsync<List<EventDto>>("api/events/mine");

    /// <summary>Loads one of the organiser's own events (the API only returns the caller's events from /mine).</summary>
    private async Task<(EventDto? Event, ApiResult? Failure)> LoadOwnedEventAsync(int eventId)
    {
        var mine = await LoadMyEventsAsync();
        if (!mine.Success || mine.Data is null) return (null, mine);

        var ev = mine.Data.FirstOrDefault(e => e.EventId == eventId);
        if (ev is null)
            return (null, new ApiResult { Success = false, StatusCode = System.Net.HttpStatusCode.NotFound, Error = "We couldn't find that event among your events." });

        return (ev, null);
    }

    private static object ToApiBody(EventFormViewModel m) => new
    {
        eventName = m.EventName,
        description = m.Description,
        eventDate = m.EventDate!.Value.ToString("yyyy-MM-dd"),
        startTime = $"{m.StartTime!.Value.Hours:00}:{m.StartTime.Value.Minutes:00}:00",
        venue = m.Venue,
        city = m.City,
        province = m.Province,
        eventType = m.EventType,
        distanceKm = m.DistanceKm,
        status = m.Status
    };

    // ---------------------------------------------------------------- dashboard

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var mine = await LoadMyEventsAsync();
        if (!mine.Success || mine.Data is null)
        {
            if (mine.IsUnauthorized) return Failed(mine);
            ViewData["Error"] = mine.Error;
            return View(new DashboardViewModel());
        }

        var model = new DashboardViewModel();
        foreach (var ev in mine.Data)
        {
            var enrolments = await Api.GetAsync<List<EnrollmentDto>>($"api/events/{ev.EventId}/enrollments");
            model.Events.Add(new DashboardEventRow
            {
                Event = ev,
                Enrolments = enrolments.Success && enrolments.Data is not null ? enrolments.Data.Count : 0
            });
        }
        return View(model);
    }

    // ---------------------------------------------------------------- events

    [HttpGet]
    public async Task<IActionResult> Events()
    {
        var mine = await LoadMyEventsAsync();
        if (!mine.Success || mine.Data is null)
        {
            if (mine.IsUnauthorized) return Failed(mine);
            ViewData["Error"] = mine.Error;
            return View(new List<EventDto>());
        }
        return View(mine.Data.OrderByDescending(e => e.EventDate).ToList());
    }

    [HttpGet]
    public IActionResult CreateEvent() => View("EventForm", new EventFormViewModel { EventDate = DateTime.Today.AddDays(30) });

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        if (!ModelState.IsValid) return View("EventForm", model);

        var created = await Api.SendAsync<EventDto>(HttpMethod.Post, "api/events", ToApiBody(model));
        if (!created.Success || created.Data is null)
        {
            if (created.IsUnauthorized) return Failed(created);
            AddApiErrors(created);
            return View("EventForm", model);
        }

        await UploadBannerAsync(created.Data.EventId, model.BannerFile);

        TempData["Success"] = $"'{created.Data.EventName}' has been created. Now add some categories to it.";
        return RedirectToAction(nameof(Categories), new { eventId = created.Data.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var (ev, failure) = await LoadOwnedEventAsync(id);
        if (ev is null) return Failed(failure!, nameof(Events), "Organiser");

        return View("EventForm", new EventFormViewModel
        {
            EventId = ev.EventId,
            EventName = ev.EventName,
            Description = ev.Description,
            EventDate = ev.EventDate.Date,
            StartTime = ev.StartTime,
            Venue = ev.Venue,
            City = ev.City,
            Province = ev.Province,
            EventType = ev.EventType,
            DistanceKm = ev.DistanceKm,
            Status = ev.Status,
            BannerUrl = ev.BannerUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(int id, EventFormViewModel model)
    {
        model.EventId = id;
        if (!ModelState.IsValid) return View("EventForm", model);

        var updated = await Api.SendAsync<EventDto>(HttpMethod.Put, $"api/events/{id}", ToApiBody(model));
        if (!updated.Success || updated.Data is null)
        {
            if (updated.IsUnauthorized || updated.IsNotFound) return Failed(updated, nameof(Events), "Organiser");
            AddApiErrors(updated);
            return View("EventForm", model);
        }

        await UploadBannerAsync(id, model.BannerFile);

        TempData["Success"] = $"'{updated.Data.EventName}' has been updated.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await Api.SendAsync(HttpMethod.Delete, $"api/events/{id}");
        if (result.IsUnauthorized) return Failed(result);

        if (result.Success) TempData["Success"] = "The event has been deleted.";
        else TempData["Error"] = ErrorText(result);
        return RedirectToAction(nameof(Events));
    }

    /// <summary>Sends the banner to the API (which stores it in Azure Blob Storage). A failure never loses the saved event.</summary>
    private async Task UploadBannerAsync(int eventId, IFormFile? file)
    {
        if (file is null || file.Length == 0) return;

        var upload = await Api.UploadAsync<ImageUploadDto>($"api/events/{eventId}/banner", file);
        if (!upload.Success)
            TempData["Warning"] = $"The event was saved, but the banner image could not be uploaded: {ErrorText(upload)}";
    }

    // ---------------------------------------------------------------- categories

    [HttpGet]
    public async Task<IActionResult> Categories(int eventId)
    {
        var (ev, failure) = await LoadOwnedEventAsync(eventId);
        if (ev is null) return Failed(failure!, nameof(Events), "Organiser");

        var model = new CategoryManagementViewModel { Event = ev };

        var offered = await Api.GetAsync<List<EventCategoryDto>>($"api/events/{eventId}/categories");
        if (offered.Success && offered.Data is not null) model.EventCategories = offered.Data;
        else ViewData["Error"] = offered.Error;

        var master = await Api.GetAsync<List<CategoryDto>>("api/categories");
        if (master.Success && master.Data is not null)
        {
            var usedIds = model.EventCategories.Select(c => c.CategoryId).ToHashSet();
            model.AvailableCategories = master.Data.Where(c => !usedIds.Contains(c.CategoryId)).ToList();
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AttachCategory(int eventId, AttachCategoryViewModel attach)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Categories), new { eventId });
        }

        var result = await Api.SendAsync(HttpMethod.Post, $"api/events/{eventId}/categories", new
        {
            categoryId = attach.CategoryId,
            entryFee = attach.EntryFee,
            maximumParticipants = attach.MaximumParticipants
        });

        if (result.IsUnauthorized) return Failed(result);
        if (result.Success) TempData["Success"] = "The category has been added to the event.";
        else TempData["Error"] = ErrorText(result);
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateEventCategory(int eventId, int eventCategoryId, decimal? entryFee, int? maximumParticipants)
    {
        if (entryFee is null || entryFee < 0 || maximumParticipants is null || maximumParticipants < 1)
        {
            TempData["Error"] = "Please enter a valid fee (0 or more) and a maximum of at least 1 participant.";
            return RedirectToAction(nameof(Categories), new { eventId });
        }

        var result = await Api.SendAsync(HttpMethod.Put, $"api/event-categories/{eventCategoryId}",
            new { entryFee, maximumParticipants });

        if (result.IsUnauthorized) return Failed(result);
        if (result.Success) TempData["Success"] = "The category has been updated.";
        else TempData["Error"] = ErrorText(result);
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveEventCategory(int eventId, int eventCategoryId)
    {
        var result = await Api.SendAsync(HttpMethod.Delete, $"api/event-categories/{eventCategoryId}");
        if (result.IsUnauthorized) return Failed(result);

        if (result.Success) TempData["Success"] = "The category has been removed from the event.";
        else TempData["Error"] = ErrorText(result);
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory(int eventId, NewCategoryViewModel category)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Categories), new { eventId });
        }

        var result = await Api.SendAsync(HttpMethod.Post, "api/categories", new
        {
            categoryName = category.CategoryName,
            distanceKm = category.DistanceKm,
            activityType = category.ActivityType,
            description = category.Description
        });

        if (result.IsUnauthorized) return Failed(result);
        if (result.Success) TempData["Success"] = $"'{category.CategoryName}' was created. You can now add it to this event.";
        else TempData["Error"] = ErrorText(result);
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    // ---------------------------------------------------------------- enrolments

    [HttpGet]
    public async Task<IActionResult> Enrolments(int eventId)
    {
        var (ev, failure) = await LoadOwnedEventAsync(eventId);
        if (ev is null) return Failed(failure!, nameof(Events), "Organiser");

        var model = new EnrolmentsViewModel { Event = ev };

        var enrolments = await Api.GetAsync<List<EnrollmentDto>>($"api/events/{eventId}/enrollments");
        if (!enrolments.Success || enrolments.Data is null)
        {
            if (enrolments.IsUnauthorized) return Failed(enrolments);
            ViewData["Error"] = enrolments.Error;
            return View(model);
        }

        var registrations = await Api.GetAsync<List<RegistrationDto>>($"api/events/{eventId}/registrations");
        var byParticipant = registrations.Success && registrations.Data is not null
            ? registrations.Data.ToDictionary(r => r.ParticipantId)
            : new Dictionary<int, RegistrationDto>();

        model.Rows = enrolments.Data
            .Select(e => new EnrolmentRow
            {
                Enrollment = e,
                Registration = byParticipant.GetValueOrDefault(e.ParticipantId)
            })
            .ToList();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateRegistration(int eventId, int userEventId, string? raceNumber, string status)
    {
        var result = await Api.SendAsync(HttpMethod.Put, $"api/user-events/{userEventId}", new { raceNumber, status });
        if (result.IsUnauthorized) return Failed(result);

        if (result.Success) TempData["Success"] = "The registration has been updated.";
        else TempData["Error"] = ErrorText(result);
        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    // ---------------------------------------------------------------- results

    [HttpGet]
    public async Task<IActionResult> Results(int eventId)
    {
        var (ev, failure) = await LoadOwnedEventAsync(eventId);
        if (ev is null) return Failed(failure!, nameof(Events), "Organiser");

        var model = new ResultsEntryViewModel { Event = ev };

        var enrolments = await Api.GetAsync<List<EnrollmentDto>>($"api/events/{eventId}/enrollments");
        if (!enrolments.Success || enrolments.Data is null)
        {
            if (enrolments.IsUnauthorized) return Failed(enrolments);
            ViewData["Error"] = enrolments.Error;
            return View(model);
        }

        // Existing results are published per event category.
        var existing = new Dictionary<int, ResultDto>();
        foreach (var categoryId in enrolments.Data.Select(e => e.EventCategoryId).Distinct())
        {
            var results = await Api.GetAsync<List<ResultDto>>($"api/event-categories/{categoryId}/results");
            if (results.Success && results.Data is not null)
                foreach (var r in results.Data) existing[r.EnrollmentId] = r;
        }

        model.Rows = enrolments.Data
            .OrderBy(e => e.CategoryName).ThenBy(e => e.ParticipantName)
            .Select(e => new ResultRow { Enrollment = e, Result = existing.GetValueOrDefault(e.EnrollmentId) })
            .ToList();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CaptureResult(int eventId, CaptureResultViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var problems = ModelState.Values.SelectMany(v => v.Errors).Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                ? "Please check the finish time (use hh:mm:ss, e.g. 00:45:12)." : e.ErrorMessage);
            TempData["Error"] = string.Join(" ", problems);
            return RedirectToAction(nameof(Results), new { eventId });
        }

        var result = await Api.SendAsync(HttpMethod.Post, $"api/enrollments/{model.EnrollmentId}/results", new
        {
            finishTime = model.FinishTime is null ? null : Format.Time(model.FinishTime),
            position = model.Position,
            averagePace = model.AveragePace,
            resultStatus = model.ResultStatus
        });

        if (result.IsUnauthorized) return Failed(result);
        if (result.Success) TempData["Success"] = "The result has been saved and published.";
        else TempData["Error"] = ErrorText(result);
        return RedirectToAction(nameof(Results), new { eventId });
    }
}
