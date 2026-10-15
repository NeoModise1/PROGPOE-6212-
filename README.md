# RaceDay

## Overview

RaceDay is a database-backed event management system for organising and
participating in running and walking events (e.g. fun runs, road races,
half marathons, and walks). It allows event organisers to publish events,
attach race categories with their own entry fees and capacity limits,
and manage participant registrations, race-day results, routes, and
weather conditions. Participants can browse upcoming events, enrol in a
specific category (e.g. "the 10km at the Johannesburg City Challenge"),
and view their own registration history and results.

The system is built around a nine-entity relational schema (Users,
Events, Categories, EventCategories, UserEvents, Enrollments, Results,
Routes, and WeatherInformation) and is exposed through a RESTful API
implemented in C#.

## Roles

RaceDay has two user roles, both stored on the `USERS` table via the
`Role` field:

### Organiser

Organisers create and manage events. They can:
- Create, update, and delete events
- Attach race categories to an event with a specific entry fee and
  maximum participant count
- Add and manage routes and weather information for their events
- Review who has registered and enrolled in their events
- Assign race numbers and update registration status
- Capture finish times, positions, and results after a race

Organisers can only manage the events they created — actions on events,
categories, routes, and results are restricted to the owning organiser.

### Participant

Participants register for and take part in events. They can:
- Browse all published events and their category offerings
- Enrol in a specific event category (which also registers them for the
  event overall and issues a race number)
- View their own registrations, enrolments, and history
- Cancel their own enrolments
- View their own personal results across past events

Participants can only view and manage their own registrations, enrolments,
and results — not those of other users.

## Repository Contents

| File | Description |
|---|---|
| `ERD_.pdf` | Entity Relationship Diagram for the RaceDay database |
| `sql/create_tables.sql` | Schema — creates all nine tables with keys and constraints |
| `sql/insert_data.sql` | Sample seed data for testing |
| `RaceDay_API_Endpoint_Plan.md` | Full REST API endpoint plan, grouped by feature area |
| `.github/workflows/validate-raceday.yml` | CI workflow that checks required files exist and validates the SQL scripts run successfully |

<img width="736" height="632" alt="image" src="https://github.com/user-attachments/assets/b2f7d392-6b69-43e5-9d68-7bc1fcf5e19d" />

YouTube Link
https://youtu.be/cPmNyfUDwCg?si=3hhBswru_47brIuZ

# RaceDay – Part 2: API

ASP.NET Core Web API (.NET 8, C#) for the RaceDay system, built from the Part 1 endpoint plan.
It uses Entity Framework Core (code-first) with SQL Server, session-based role authentication,
Swagger, xUnit tests and a GitHub Actions CI pipeline.

## Contents

```
Part2-API/
├── RaceDay.sln
├── RaceDay.Api/      ASP.NET Core Web API
└── RaceDay.Tests/    xUnit tests
.github/workflows/ci.yml   GitHub Actions build + test workflow
```

## Prerequisites

- Visual Studio 2022 (17.8+) with the "ASP.NET and web development" workload, or the .NET 8 SDK
- SQL Server LocalDB (installed with Visual Studio) or any SQL Server instance
- EF Core tools: `dotnet tool install --global dotnet-ef`

## Setup and run instructions

1. Clone the repository and open `Part2-API/RaceDay.sln`.
2. Check the connection string in `Part2-API/RaceDay.Api/appsettings.json` (`DefaultConnection`).
   The default uses SQL Server LocalDB: `(localdb)\MSSQLLocalDB`.
3. Create the database from the code-first model (run from the `Part2-API` folder):
   ```
   dotnet ef migrations add InitialCreate --project RaceDay.Api
   dotnet ef database update --project RaceDay.Api
   ```
   (The API also applies migrations automatically when it starts.)
4. Run the API: press **F5** in Visual Studio, or
   ```
   dotnet run --project RaceDay.Api
   ```
5. Swagger UI opens at **http://localhost:5080/swagger**.
6. Run the unit tests: `dotnet test` (or Test Explorer in Visual Studio).

### Using the API through Swagger

1. `POST /api/auth/register` – create an account (see roles below).
2. `POST /api/auth/login` – this starts the session. Swagger UI sends the session cookie automatically.
3. Call the protected endpoints. `POST /api/auth/logout` ends the session.

## The two roles

| | **Organiser** | **Participant** |
|---|---|---|
| Registration | Needs the secret organiser code (`Registration:OrganiserCode` in `appsettings.json`, default `RACEDAY-ORG-2026`) so users cannot simply make themselves an Organiser | Self-registration |
| Profile | View / update own | View / update own |
| Events | Create, update, delete (own events); view all | View all |
| Categories | Create, update, delete for own events; view | View |
| Enrolments | View all enrolments for own events | Enrol in an event by choosing a category; view own enrolments |
| Results | Capture / correct finish time and position; view results for own events | View own results |

Unauthenticated requests receive **401**; a logged-in user with the wrong role receives **403**.

## Endpoints

| Method | Route | Access |
|---|---|---|
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |
| POST | `/api/auth/logout` | Public |
| GET / PUT | `/api/profile` | Any logged-in user |
| GET | `/api/events`, `/api/events/{id}` | Any logged-in user |
| POST / PUT / DELETE | `/api/events`, `/api/events/{id}` | Organiser |
| GET | `/api/events/{eventId}/categories` | Any logged-in user |
| POST / PUT / DELETE | `/api/events/{eventId}/categories[/{id}]` | Organiser |
| POST | `/api/events/{eventId}/enrolments` | Participant |
| GET | `/api/events/{eventId}/enrolments` | Organiser |
| GET | `/api/enrolments/mine` | Participant |
| POST / PUT | `/api/enrolments/{enrolmentId}/result` | Organiser |
| GET | `/api/events/{eventId}/results` | Organiser |
| GET | `/api/results/mine` | Participant |

## Security notes

- Passwords are hashed with PBKDF2 (SHA-256, 100 000 iterations, random salt). The original password is never stored.
- Authentication state and role are kept in a server-side session (HttpOnly session cookie, 30 min idle timeout).
- Organisers can only modify or view data belonging to their own events.

## Unit tests

`RaceDay.Tests` runs the real API in memory (`WebApplicationFactory`) with an in-memory database and covers
registration/login, event management, role rejection (401/403), enrolments, results and profiles
(success and failure scenarios).

## CI/CD

`.github/workflows/ci.yml` restores, builds and runs all unit tests on every push.

**Green build screenshot:**



## Demo video

YouTube (unlisted): 

