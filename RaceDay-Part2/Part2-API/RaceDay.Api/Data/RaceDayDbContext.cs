using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<RaceResult> Results => Set<RaceResult>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<Event>(e =>
        {
            e.Property(x => x.EventType).HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.DistanceKm).HasPrecision(8, 2);
            e.HasOne(x => x.Organiser).WithMany().HasForeignKey(x => x.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Category>(e =>
        {
            e.Property(x => x.DistanceKm).HasPrecision(8, 2);
            e.HasOne(x => x.Event).WithMany(x => x.Categories).HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.EventId, x.Name }).IsUnique();
        });

        // Only ONE cascade path (via Event) to avoid SQL Server "multiple cascade paths" errors.
        b.Entity<Enrolment>(e =>
        {
            e.HasOne(x => x.Event).WithMany(x => x.Enrolments).HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            // A participant can enter an event only once.
            e.HasIndex(x => new { x.UserId, x.EventId }).IsUnique();
        });

        b.Entity<RaceResult>(e =>
        {
            e.HasOne(x => x.Enrolment).WithOne(x => x.Result).HasForeignKey<RaceResult>(x => x.EnrolmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
