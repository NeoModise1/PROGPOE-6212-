using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password."), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "First name is required."), StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required."), StringLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Please enter a valid email address."), StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20), Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Password is required."), StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password."), DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose how you will use RaceDay.")]
    [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Please choose Organiser or Participant.")]
    public string Role { get; set; } = "Participant";
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "First name is required."), StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required."), StringLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20), Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}

public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Event name is required."), StringLength(150)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date.")]
    [Display(Name = "Date")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the start time.")]
    [Display(Name = "Start time")]
    public TimeSpan? StartTime { get; set; }

    [Required(ErrorMessage = "Venue is required."), StringLength(200)]
    public string Venue { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required."), StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Province is required."), StringLength(100)]
    public string Province { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose an event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Required(ErrorMessage = "Please enter the distance in km."), Range(0.01, 9999, ErrorMessage = "Distance must be between 0.01 and 9999 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    public string Status { get; set; } = "Scheduled";

    public string? BannerUrl { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? BannerFile { get; set; }

    public static readonly string[] EventTypes = { "Run", "Walk", "Cycle", "Multisport" };
    public static readonly string[] Statuses = { "Scheduled", "Ongoing", "Completed", "Cancelled" };
}

public class AttachCategoryViewModel
{
    [Required(ErrorMessage = "Please choose a category.")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Please enter the entry fee."), Range(0, 100000, ErrorMessage = "Fee must be between 0 and 100 000.")]
    public decimal? EntryFee { get; set; }

    [Required(ErrorMessage = "Please enter the maximum participants."), Range(1, 1000000, ErrorMessage = "Maximum must be at least 1.")]
    public int? MaximumParticipants { get; set; }
}

public class NewCategoryViewModel
{
    [Required(ErrorMessage = "Category name is required."), StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Distance is required."), Range(0.01, 9999, ErrorMessage = "Distance must be between 0.01 and 9999 km.")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Please choose an activity type.")]
    public string ActivityType { get; set; } = "Run";

    [StringLength(500)]
    public string? Description { get; set; }
}

public class CaptureResultViewModel
{
    [Required] public int EnrollmentId { get; set; }
    [Required(ErrorMessage = "Please choose a result status.")] public string ResultStatus { get; set; } = "Completed";
    public TimeSpan? FinishTime { get; set; }
    [Range(1, 1000000, ErrorMessage = "Position must be 1 or higher.")] public int? Position { get; set; }
    [Range(0, 9999, ErrorMessage = "Pace must be 0 or higher.")] public decimal? AveragePace { get; set; }
}

// ---------- Page models ----------

public class HomeViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public List<string> Provinces { get; set; } = new();
    public string? Search { get; set; }
    public string? Province { get; set; }
    public string? EventType { get; set; }
    public DateTime? From { get; set; }
    public int TotalUpcoming { get; set; }
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EventCategoryDto> Categories { get; set; } = new();
    public List<RouteDto> Routes { get; set; } = new();
    public List<WeatherDto> Weather { get; set; } = new();
    public HashSet<int> EnrolledCategoryIds { get; set; } = new();
}

public class DashboardEventRow
{
    public EventDto Event { get; set; } = new();
    public int Enrolments { get; set; }
}

public class DashboardViewModel
{
    public List<DashboardEventRow> Events { get; set; } = new();
    public int TotalEvents => Events.Count;
    public int TotalEnrolments => Events.Sum(e => e.Enrolments);
    public List<DashboardEventRow> Upcoming => Events.Where(e => e.Event.IsUpcoming).OrderBy(e => e.Event.EventDate).ToList();
}

public class CategoryManagementViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EventCategoryDto> EventCategories { get; set; } = new();
    public List<CategoryDto> AvailableCategories { get; set; } = new();
    public AttachCategoryViewModel Attach { get; set; } = new();
    public NewCategoryViewModel NewCategory { get; set; } = new();
}

public class EnrolmentRow
{
    public EnrollmentDto Enrollment { get; set; } = new();
    public RegistrationDto? Registration { get; set; }
}

public class EnrolmentsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentRow> Rows { get; set; } = new();
}

public class ResultRow
{
    public EnrollmentDto Enrollment { get; set; } = new();
    public ResultDto? Result { get; set; }
}

public class ResultsEntryViewModel
{
    public EventDto Event { get; set; } = new();
    public List<ResultRow> Rows { get; set; } = new();
    public string[] Statuses { get; } = { "Completed", "DNF", "DNS", "Disqualified" };
}

public class MyEventRow
{
    public EnrollmentDto Enrollment { get; set; } = new();
    public EventDto? Event { get; set; }
    public RegistrationDto? Registration { get; set; }
}

public class MyResultRow
{
    public ResultDto Result { get; set; } = new();
    public DateTime? EventDate { get; set; }
    public int? TotalFinishers { get; set; }
}
