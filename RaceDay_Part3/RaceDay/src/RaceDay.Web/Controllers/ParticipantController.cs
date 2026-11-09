using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(Roles = "Participant")]
public class ParticipantController : AppController
{
    public ParticipantController(IRaceDayApi api) : base(api) { }

    /// <summary>Enrols the logged-in participant in a category; the API records the enrolment immediately.</summary>
    [HttpPost]
    public async Task<IActionResult> Enrol(int eventId, int eventCategoryId)
    {
        var result = await Api.SendAsync<EnrollmentDto>(HttpMethod.Post, $"api/event-categories/{eventCategoryId}/enrollments");
        if (result.IsUnauthorized) return Failed(result);

        if (!result.Success)
        {
            TempData["Error"] = ErrorText(result);
            return RedirectToAction("Details", "Home", new { id = eventId });
        }

        TempData["Success"] = $"You're in! You have enrolled for {result.Data?.EventName} ({result.Data?.CategoryName}).";
        return RedirectToAction(nameof(MyEvents));
    }

    [HttpGet]
    public async Task<IActionResult> MyEvents()
    {
        var enrolments = await Api.GetAsync<List<EnrollmentDto>>("api/users/me/enrollments");
        if (!enrolments.Success || enrolments.Data is null)
        {
            if (enrolments.IsUnauthorized) return Failed(enrolments);
            ViewData["Error"] = enrolments.Error;
            return View(new List<MyEventRow>());
        }

        var events = await Api.GetAsync<List<EventDto>>("api/events");
        var eventsById = events.Success && events.Data is not null
            ? events.Data.ToDictionary(e => e.EventId)
            : new Dictionary<int, EventDto>();

        var registrations = await Api.GetAsync<List<RegistrationDto>>("api/users/me/events");
        var registrationsByEvent = registrations.Success && registrations.Data is not null
            ? registrations.Data.ToDictionary(r => r.EventId)
            : new Dictionary<int, RegistrationDto>();

        var rows = enrolments.Data
            .Select(e => new MyEventRow
            {
                Enrollment = e,
                Event = eventsById.GetValueOrDefault(e.EventId),
                Registration = registrationsByEvent.GetValueOrDefault(e.EventId)
            })
            .OrderByDescending(r => r.Event?.EventDate ?? DateTime.MinValue)
            .ToList();

        return View(rows);
    }

    [HttpPost]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var result = await Api.SendAsync(HttpMethod.Delete, $"api/enrollments/{id}");
        if (result.IsUnauthorized) return Failed(result);

        if (result.Success) TempData["Success"] = "Your enrolment has been cancelled.";
        else TempData["Error"] = ErrorText(result);
        return RedirectToAction(nameof(MyEvents));
    }

    /// <summary>Personal race history: event, date, category, finish time and finishing position.</summary>
    [HttpGet]
    public async Task<IActionResult> MyResults()
    {
        var results = await Api.GetAsync<List<ResultDto>>("api/users/me/results");
        if (!results.Success || results.Data is null)
        {
            if (results.IsUnauthorized) return Failed(results);
            ViewData["Error"] = results.Error;
            return View(new List<MyResultRow>());
        }

        // Join each result to its event date (via the participant's enrolments) ...
        var enrolments = await Api.GetAsync<List<EnrollmentDto>>("api/users/me/enrollments");
        var events = await Api.GetAsync<List<EventDto>>("api/events");
        var eventDates = events.Success && events.Data is not null
            ? events.Data.ToDictionary(e => e.EventId, e => e.EventDate)
            : new Dictionary<int, DateTime>();
        var eventIdByEnrollment = enrolments.Success && enrolments.Data is not null
            ? enrolments.Data.ToDictionary(e => e.EnrollmentId, e => e.EventId)
            : new Dictionary<int, int>();

        // ... and count the finishers in each category so we can show e.g. "47th of 312".
        var finishers = new Dictionary<int, int>();
        foreach (var categoryId in results.Data.Select(r => r.EventCategoryId).Distinct())
        {
            var category = await Api.GetAsync<List<ResultDto>>($"api/event-categories/{categoryId}/results");
            if (category.Success && category.Data is not null)
                finishers[categoryId] = category.Data.Count(r => r.ResultStatus == "Completed");
        }

        var rows = results.Data.Select(r => new MyResultRow
        {
            Result = r,
            EventDate = eventIdByEnrollment.TryGetValue(r.EnrollmentId, out var eventId) && eventDates.TryGetValue(eventId, out var date)
                ? date : null,
            TotalFinishers = finishers.TryGetValue(r.EventCategoryId, out var total) && total > 0 ? total : null
        })
        .OrderByDescending(r => r.EventDate ?? DateTime.MinValue)
        .ToList();

        return View(rows);
    }
}
