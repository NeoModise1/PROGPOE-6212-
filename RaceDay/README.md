# RaceDay API (PROG6212 POE - Part 2)

ASP.NET Core 8 Web API for the RaceDay event management system, built to the Part 1 endpoint plan and ERD.

- **Stack:** ASP.NET Core Web API (C#), Entity Framework Core (Code-First) + SQL Server, JWT authentication, Swagger UI, xUnit tests, GitHub Actions CI.
- **Roles:** `Organiser` and `Participant` (role-based access enforced with `[Authorize(Roles = ...)]` plus ownership checks).

## Roles

| Role | Can do |
|------|--------|
| **Organiser** | Create/update/delete **own** events; manage the category master list; attach categories (fee + capacity) to own events; view enrolments and registrations for own events; assign race numbers; capture results; manage routes and weather for own events. |
| **Participant** | View events, categories, routes, weather and results; enrol in / cancel own enrolments in an event category; view own enrolments, registrations and results. |
| **Public (no login)** | Register, log in, browse events/categories/routes/weather/category results. |

Any endpoint a role is not allowed to use returns **403 Forbidden**; missing/invalid token returns **401 Unauthorized**.

## Prerequisites

- .NET 8 SDK (or newer SDK; the projects target `net8.0`)
- SQL Server LocalDB (installed with Visual Studio) or any SQL Server instance

## Setup and run

1. Open `RaceDay.sln` in Visual Studio (or use the CLI below).
2. Check the connection string in `src/RaceDay.Api/appsettings.json` (`ConnectionStrings:DefaultConnection`). The default uses LocalDB:
   `Server=(localdb)\MSSQLLocalDB;Database=RaceDayDb;Trusted_Connection=True;TrustServerCertificate=True`
3. Run:
   ```bash
   dotnet restore
   dotnet run --project src/RaceDay.Api
   ```
4. Open **http://localhost:5080** - Swagger UI loads at the site root.

The database schema is created automatically on first start (Code-First, matches the ERD) and the master `CATEGORIES` list is seeded.

**Using the SQL script instead:** `database/RaceDay_Schema_and_Seed.sql` creates the same schema (identical table and column names to the ERD and the EF model) plus sample data for `RaceDayDb`. Sample users (password `Password123!`): `thabo.mokoena@raceday.co.za` and `lerato.dlamini@raceday.co.za` (Organisers), `sipho.ndlovu@example.com` and `aisha.naidoo@example.com` (Participants).

> Always start from an **empty** database or run the script (it drops and recreates the tables). If an older database with different tables already exists, `EnsureCreated()` will not fix it - delete the database first.

> Prefer migrations? Run `dotnet ef migrations add InitialCreate --project src/RaceDay.Api` and replace
> `db.Database.EnsureCreated()` in `Program.cs` with `db.Database.Migrate()`.

### Using Swagger with authentication

1. `POST /api/auth/register` (choose role `Organiser` or `Participant`).
2. `POST /api/auth/login` and copy the `token` from the response.
3. Click **Authorize** (top right), paste the token, and test the protected endpoints.

## Running the unit tests

```bash
dotnet test
```

Tests use the real API in memory (`WebApplicationFactory`) with an isolated EF Core in-memory database, and cover authenticated and unauthenticated requests, success and failure cases, and role enforcement for: registration/login, user profile, events, categories, event categories, enrolments, registrations, results, routes and weather, plus Swagger generation.

## CI/CD

`.github/workflows/ci.yml` builds the solution and runs all tests on every push and pull request.

> If you place this project in a **subfolder** of your GitHub repository, move `.github/workflows/ci.yml` to the repository root `.github/workflows/` and prefix the paths, e.g. `dotnet test <subfolder>/RaceDay.sln`.

**CI screenshot (green build):** _add screenshot here_

## Demo video

**YouTube (unlisted):** _add link here_

## Project structure

```
RaceDay.sln
src/RaceDay.Api/
  Controllers/   one controller per area of the endpoint plan
  Models/        EF Core entities (match the ERD)
  Data/          DbContext, relationships, seed data
  Dtos/          request/response models and mapping
  Services/      password hashing (PBKDF2) and JWT creation
Tests: tests/RaceDay.Tests/
database/RaceDay_Schema_and_Seed.sql   ERD-matching schema + sample data
.github/workflows/ci.yml
```

## Endpoints (from the Part 1 plan)

Authentication, User Profile, Events, Categories, Event Categories, Enrollments, User Events (registrations), Results, Routes and Weather Information - all endpoints in the plan are implemented and visible in Swagger with descriptions, request bodies and responses.

## Notes / design decisions

- Passwords are hashed with PBKDF2 (random salt) and never stored in plain text.
- Authentication uses JWT bearer tokens (as specified in the endpoint plan); the role is carried in the token and checked on every request.
- Enrolling creates the participant's event registration (`UserEvents`); cancelling the last enrolment for an event removes it.
- AI assistance was used to help generate this project; review and understand the code before submitting.
