using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<RaceEvent> Events => Set<RaceEvent>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventCategory> EventCategories => Set<EventCategory>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<UserEvent> UserEvents => Set<UserEvent>();
    public DbSet<RaceResult> Results => Set<RaceResult>();
    public DbSet<RaceRoute> Routes => Set<RaceRoute>();
    public DbSet<WeatherInformation> WeatherInformation => Set<WeatherInformation>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<AppUser>(e =>
        {
            e.ToTable("USERS");
            e.HasKey(x => x.UserID);
            e.Property(x => x.FirstName).IsRequired().HasMaxLength(50);
            e.Property(x => x.LastName).IsRequired().HasMaxLength(50);
            e.Property(x => x.Email).IsRequired().HasMaxLength(100);
            e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(255);
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.Role).IsRequired().HasMaxLength(20);
            e.HasIndex(x => x.Email).IsUnique();

            e.HasOne(x => x.Organiser).WithOne(o => o.User)
                .HasForeignKey<Organiser>(o => o.UserID).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Participant).WithOne(p => p.User)
                .HasForeignKey<Participant>(p => p.UserID).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Organiser>(e =>
        {
            e.ToTable("ORGANISERS");
            e.HasKey(x => x.OrganiserID);
        });

        mb.Entity<Participant>(e =>
        {
            e.ToTable("PARTICIPANTS");
            e.HasKey(x => x.ParticipantID);
        });

        mb.Entity<RaceEvent>(e =>
        {
            e.ToTable("EVENTS");
            e.HasKey(x => x.EventID);
            e.Property(x => x.EventName).IsRequired().HasMaxLength(150);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.StartTime).HasColumnType("time");
            e.Property(x => x.Venue).IsRequired().HasMaxLength(200);
            e.Property(x => x.City).IsRequired().HasMaxLength(100);
            e.Property(x => x.Province).IsRequired().HasMaxLength(100);
            e.Property(x => x.Status).IsRequired().HasMaxLength(30);
            e.HasOne(x => x.Organiser).WithMany(o => o.Events)
                .HasForeignKey(x => x.OrganiserID).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<Category>(e =>
        {
            e.ToTable("CATEGORIES");
            e.HasKey(x => x.CategoryID);
            e.Property(x => x.CategoryName).IsRequired().HasMaxLength(100);
            e.Property(x => x.DistanceKM).HasPrecision(6, 2);
            e.Property(x => x.ActivityType).IsRequired().HasMaxLength(30);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasIndex(x => new { x.CategoryName, x.DistanceKM }).IsUnique();
        });

        mb.Entity<EventCategory>(e =>
        {
            e.ToTable("EVENT_CATEGORIES");
            e.HasKey(x => x.EventCategoryID);
            e.Property(x => x.EntryFee).HasPrecision(10, 2);
            e.HasIndex(x => new { x.EventID, x.CategoryID }).IsUnique();
            e.HasOne(x => x.Event).WithMany(ev => ev.EventCategories)
                .HasForeignKey(x => x.EventID).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category).WithMany(c => c.EventCategories)
                .HasForeignKey(x => x.CategoryID).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<Enrollment>(e =>
        {
            e.ToTable("ENROLLMENTS");
            e.HasKey(x => x.EnrollmentID);
            e.Property(x => x.Status).IsRequired().HasMaxLength(30);
            e.HasIndex(x => new { x.ParticipantID, x.EventCategoryID }).IsUnique();
            e.HasOne(x => x.Participant).WithMany(p => p.Enrollments)
                .HasForeignKey(x => x.ParticipantID).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.EventCategory).WithMany(ec => ec.Enrollments)
                .HasForeignKey(x => x.EventCategoryID).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<UserEvent>(e =>
        {
            e.ToTable("USER_EVENTS");
            e.HasKey(x => x.UserEventID);
            e.Property(x => x.RaceNumber).HasMaxLength(30);
            e.Property(x => x.Status).IsRequired().HasMaxLength(30);
            e.HasIndex(x => new { x.ParticipantID, x.EventID }).IsUnique();
            e.HasOne(x => x.Participant).WithMany(p => p.UserEvents)
                .HasForeignKey(x => x.ParticipantID).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Event).WithMany(ev => ev.UserEvents)
                .HasForeignKey(x => x.EventID).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<RaceResult>(e =>
        {
            e.ToTable("RESULTS");
            e.HasKey(x => x.ResultID);
            e.Property(x => x.AveragePace).HasPrecision(6, 2);
            e.Property(x => x.ResultStatus).IsRequired().HasMaxLength(30);
            e.HasOne(x => x.Enrollment).WithOne(en => en.Result)
                .HasForeignKey<RaceResult>(x => x.EnrollmentID).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<RaceRoute>(e =>
        {
            e.ToTable("ROUTES");
            e.HasKey(x => x.RouteID);
            e.Property(x => x.RouteName).IsRequired().HasMaxLength(150);
            e.Property(x => x.DistanceKM).HasPrecision(6, 2);
            e.Property(x => x.ElevationGain).HasPrecision(6, 2);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.MapURL).HasMaxLength(500);
            e.HasOne(x => x.Event).WithMany(ev => ev.Routes)
                .HasForeignKey(x => x.EventID).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<WeatherInformation>(e =>
        {
            e.ToTable("WEATHER_INFORMATION");
            e.HasKey(x => x.WeatherID);
            e.Property(x => x.Temperature).HasPrecision(5, 2);
            e.Property(x => x.WeatherCondition).IsRequired().HasMaxLength(100);
            e.Property(x => x.WindSpeed).HasPrecision(6, 2);
            e.Property(x => x.Humidity).HasPrecision(5, 2);
            e.HasOne(x => x.Event).WithMany(ev => ev.WeatherRecords)
                .HasForeignKey(x => x.EventID).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
