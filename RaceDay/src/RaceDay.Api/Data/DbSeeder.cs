using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>Seeds the master list of category types the first time the database is created (same list as the SQL script).</summary>
public static class DbSeeder
{
    public static void Seed(RaceDayDbContext db)
    {
        if (db.Categories.Any()) return;

        db.Categories.AddRange(
            new Category { CategoryName = "5 KM Fun Run", DistanceKM = 5m, ActivityType = "Run", Description = "A beginner-friendly five kilometre road race." },
            new Category { CategoryName = "10 KM Road Race", DistanceKM = 10m, ActivityType = "Run", Description = "A standard ten kilometre competitive road race." },
            new Category { CategoryName = "21 KM Half Marathon", DistanceKM = 21.10m, ActivityType = "Run", Description = "A half-marathon distance race for experienced runners." },
            new Category { CategoryName = "5 KM Walk", DistanceKM = 5m, ActivityType = "Walk", Description = "A recreational five kilometre walking category." },
            new Category { CategoryName = "20 KM Cycle", DistanceKM = 20m, ActivityType = "Cycle", Description = "A twenty kilometre cycle." },
            new Category { CategoryName = "40 KM Cycle", DistanceKM = 40m, ActivityType = "Cycle", Description = "A forty kilometre cycle." });

        db.SaveChanges();
    }
}
