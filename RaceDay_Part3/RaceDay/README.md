# RaceDay (PROG6212 POE - Parts 2 and 3)

RaceDay is an event-management platform for running, walking and cycling events.

| Project | Description |
|---------|-------------|
| `src/RaceDay.Api` | **Part 2** - ASP.NET Core 8 Web API (EF Core + SQL Server, JWT, Swagger). Part 3 adds **Azure Blob Storage** image uploads. |
| `src/RaceDay.Web` | **Part 3** - ASP.NET Core 8 **MVC** web front end. Consumes the API only - no database access, no business logic, no direct Azure access. |
| `tests/RaceDay.Tests` | xUnit tests for the API (blob storage is replaced by an in-memory fake). |

- **Demo video (unlisted YouTube):** _add link here_
- **CI (green build):** _add screenshot here_

## Quick start - everything in one command (Docker)

Requires Docker Desktop. From the repository root:

```bash
docker compose up --build
```

This starts SQL Server, **Azurite** (a local Azure Blob Storage emulator), the API and the MVC app. The first start takes a minute while SQL Server initialises and the database is created and seeded.

| What | URL |
|------|-----|
| **RaceDay web app (MVC)** | http://localhost:5090 |
| API + Swagger | http://localhost:5080 |
| Blob storage emulator | http://localhost:10000 |

Stop with `Ctrl+C`; `docker compose down -v` also removes the data volumes.

The containerised app behaves the same as running locally. To run **only the MVC container** against an API that is already running on your machine:

```bash
docker build -f src/RaceDay.Web/Dockerfile -t raceday-web .
docker run --rm -p 5090:8080 -e Api__BaseUrl=http://host.docker.internal:5080 raceday-web
```

### Sample logins (seed script)

Run `database/RaceDay_Schema_and_Seed.sql` if you want the sample data, or simply register new users on the **Register** page (choose *Organiser* or *Participant*).
Sample users in the script (password `Password123!`): `thabo.mokoena@raceday.co.za` (Organiser), `sipho.ndlovu@example.com` (Participant).

## Running locally without Docker

1. Start the API: `dotnet run --project src/RaceDay.Api` (http://localhost:5080).
2. Start the MVC app: `dotnet run --project src/RaceDay.Web` (http://localhost:5090). It reads the API address from `Api:BaseUrl` in `src/RaceDay.Web/appsettings.json`.
3. For image uploads set `BlobStorage:ConnectionString` in the API's `appsettings.json` (user secrets recommended) - either a real Azure Storage connection string, or `UseDevelopmentStorage=true` with Azurite running (`npx azurite-blob`). Without it everything works except uploads, which show a friendly "temporarily unavailable" message.

> The API now has extra columns (`EVENTS.EventType`, `EVENTS.DistanceKM`, `EVENTS.BannerUrl`, `USERS.ProfilePictureUrl`). `EnsureCreated()` will not alter an existing database - **delete the old `RaceDayDb`** (or run the updated SQL script) first.

## Part 3 - how the requirements are met

### 1. MVC application setup
`RaceDay.Web` talks to the API through one typed client (`Services/RaceDayApi.cs`). There is no `DbContext`, no connection string and no Azure SDK in the MVC project.

### 2. Authentication and role-based navigation
- Register and log in call `POST /api/auth/register` and `POST /api/auth/login`.
- The JWT, user id, role and name are stored **server-side in the ASP.NET Core session** (`Program.cs`, `Services/SessionUser.cs`); the browser only holds a session cookie.
- `[SessionAuthorize(Roles = "...")]` (`Filters/SessionAuthorizeAttribute.cs`) protects every private page: guests are redirected to the login page (and back after logging in); the wrong role gets an *Access denied* page.
- The navigation bar differs per role. **Log out** clears the session and returns to the login page. If the API rejects an expired token the session is cleared and the user is sent to log in again.

### 3. Organiser interface
| Page | Route |
|------|-------|
| Dashboard (events, enrolments per event, upcoming dates) | `/Organiser/Dashboard` |
| Events management (create / edit / delete, banner upload) | `/Organiser/Events` |
| Category management (add age or distance categories, fee, capacity) | `/Organiser/Categories?eventId=` |
| Enrolments (participant, category, status, race numbers) | `/Organiser/Enrolments?eventId=` |
| Results entry (finish time + position, published immediately) | `/Organiser/Results?eventId=` |

### 4. Participant interface
| Page | Route |
|------|-------|
| Home - upcoming events with filters (search, province, type, date) | `/` |
| Event details (info, categories, route, weather, banner) | `/Home/Details/{id}` |
| Enrol in a category (recorded immediately) | button on the details page |
| My enrolments + status (cancel an enrolment) | `/Participant/MyEvents` |
| My Results - event, date, category, time, position e.g. *47th out of 312 finishers* | `/Participant/MyResults` |
| Profile - update details and upload a profile picture | `/Account/Profile` |

### 5. Azure Blob Storage (in the API)
- `POST /api/events/{id}/banner` (owning Organiser) and `POST /api/users/me/picture` (any logged-in user) accept `multipart/form-data` (JPEG/PNG/GIF/WebP, max 5 MB), store the image in a public-read blob container and save the URL on the event / user.
- The MVC app only posts the file to the API (`RaceDayApi.UploadAsync`) and displays the returned URL - it never talks to Azure.
- Configuration (`BlobStorage` section): `ConnectionString`, `ContainerName`, `PublicBaseUrl` (only needed for Azurite in Docker). Replacing a banner/picture deletes the old blob.

### 6. Docker
- Multi-stage `Dockerfile`s for the API and MVC app (restore layer cached, SDK image to build, small `aspnet` runtime image, non-root user).
- `docker-compose.yml` wires SQL Server, Azurite, the API and the MVC app so the whole platform starts with **`docker compose up --build`**.

### 7. UI design
Bootstrap 5.3 + a custom sporting theme (`wwwroot/css/site.css`: navy / electric orange, Barlow Condensed headings). One layout, one stylesheet and shared partials keep fonts, colours, spacing and components identical on every page. Statuses are colour-coded everywhere: **green** Confirmed/Completed, **amber** Registered/Pending, **red** Cancelled/DNS/Disqualified, **orange** DNF. Invalid input shows inline messages, API failures are turned into friendly alerts, and unexpected errors land on a friendly error page - raw errors are never shown.

### 8. GitHub and CI/CD
`.github/workflows/ci.yml` restores, builds the **whole solution (API, MVC project and tests)**, runs all tests and then builds both Docker images.

## Project structure

```
RaceDay.sln
docker-compose.yml
src/RaceDay.Api/   Controllers, Models, Data, Dtos, Services (JWT, passwords, Blob Storage), Dockerfile
src/RaceDay.Web/   Controllers, Models (API DTOs + view models), Services (API client, session), Filters, Views, wwwroot, Dockerfile
tests/RaceDay.Tests/
database/RaceDay_Schema_and_Seed.sql
.github/workflows/ci.yml
```

---

# RaceDay API (Part 2 reference)

## API roles

| Role | Can do |
|------|--------|
| **Organiser** | Create/update/delete **own** events; manage the category master list; attach categories (fee + capacity) to own events; view enrolments and registrations for own events; assign race numbers; capture results; manage routes and weather for own events. |
| **Participant** | View events, categories, routes, weather and results; enrol in / cancel own enrolments in an event category; view own enrolments, registrations and results. |
| **Public (no login)** | Register, log in, browse events/categories/routes/weather/category results. |

Any endpoint a role is not allowed to use returns **403 Forbidden**; missing/invalid token returns **401 Unauthorized**.

### Prerequisites (running without Docker)

- .NET 8 SDK (or newer SDK; the projects target `net8.0`)
- SQL Server LocalDB (installed with Visual Studio) or any SQL Server instance

### API setup and run

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

#### Using Swagger with authentication

1. `POST /api/auth/register` (choose role `Organiser` or `Participant`).
2. `POST /api/auth/login` and copy the `token` from the response.
3. Click **Authorize** (top right), paste the token, and test the protected endpoints.

### Running the unit tests

```bash
dotnet test
```

Tests use the real API in memory (`WebApplicationFactory`) with an isolated EF Core in-memory database, and cover authenticated and unauthenticated requests, success and failure cases, and role enforcement for: registration/login, user profile, events, categories, event categories, enrolments, registrations, results, routes and weather, plus Swagger generation.

### CI/CD

`.github/workflows/ci.yml` builds the solution and runs all tests on every push and pull request.

> If you place this project in a **subfolder** of your GitHub repository, move `.github/workflows/ci.yml` to the repository root `.github/workflows/` and prefix the paths, e.g. `dotnet test <subfolder>/RaceDay.sln`.

## Endpoints (from the Part 1 plan)

Authentication, User Profile, Events, Categories, Event Categories, Enrollments, User Events (registrations), Results, Routes and Weather Information - all endpoints in the plan are implemented and visible in Swagger with descriptions, request bodies and responses.

## Notes / design decisions

- Passwords are hashed with PBKDF2 (random salt) and never stored in plain text.
- The MVC app keeps the API's JWT in the server-side session; the role is carried in the token and checked on every API request.
- Enrolling creates the participant's event registration (`UserEvents`); cancelling the last enrolment for an event removes it.
- `GET /api/events/mine` (Organiser) was added in Part 3 so the MVC app can list the logged-in organiser's own events.
- AI assistance was used to help generate this project; review and understand the code before submitting.
