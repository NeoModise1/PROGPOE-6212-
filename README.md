# RaceDay PROG6212 POE - Parts 1, 2 & 3

RaceDay is an event management system for running, walking and cycling race events. This repository contains all three parts of the POE:

| Part | What it covers | Where it lives |
|------|----------------|----------------|
| **Part 1 - Design** | Endpoint plan, ERD and SQL schema | `schema.sql`, plus the Part 1 documents linked below |
| **Part 2 - Implementation** | ASP.NET Core 8 Web API built to the Part 1 endpoint plan and ERD, with unit tests and CI | `RaceDay/src/RaceDay.Api` |
| **Part 3 - MVC front end** | ASP.NET Core 8 MVC web app that consumes the API, Azure Blob Storage image uploads, Docker and CI/CD | `RaceDay/src/RaceDay.Web` (plus blob storage in the API) |

- **Demo video (unlisted YouTube):** https://youtu.be/Moc6l6zoZeo?si=G0gp8eTCEuF2I80E 
- **CI (green build):** see [CI/CD](#cicd)

## Quick start - everything in one command (Docker)

Requires Docker Desktop. From the `RaceDay/` folder (the one containing `docker-compose.yml`):

```bash
docker compose up --build
```

This starts SQL Server, **Azurite** (a local Azure Blob Storage emulator), the API and the MVC app. The first start takes a minute or two while SQL Server initialises and the database is created and seeded.

| What | URL |
|------|-----|
| **RaceDay web app (MVC)** | http://localhost:5090 |
| API + Swagger | http://localhost:5080 |
| Blob storage emulator | http://localhost:10000 |

Stop with `Ctrl+C`; `docker compose down -v` also removes the data volumes. Next time, `docker compose up` is enough (no `--build` unless the code changed).

To run **only the MVC container** against an API that is already running on your machine:

```bash
docker build -f src/RaceDay.Web/Dockerfile -t raceday-web .
docker run --rm -p 5090:8080 -e Api__BaseUrl=http://host.docker.internal:5080 raceday-web
```

### Sample logins

Run `RaceDay/database/RaceDay_Schema_and_Seed.sql` for the sample data, or simply register new users on the **Register** page (choose *Organiser* or *Participant*). Sample users in the script (password `Password123!`):

| Role | Email |
|------|-------|
| Organiser | `thabo.mokoena@raceday.co.za` |
| Participant | `sipho.ndlovu@example.com` |

## Running locally without Docker

Prerequisites: .NET 8 SDK and SQL Server LocalDB (installed with Visual Studio) or any SQL Server instance.

Run both projects from the `RaceDay/` folder:

1. Start the API: `dotnet run --project src/RaceDay.Api` (http://localhost:5080).
2. Start the MVC app: `dotnet run --project src/RaceDay.Web` (http://localhost:5090). It reads the API address from `Api:BaseUrl` in `src/RaceDay.Web/appsettings.json`.
3. **Visual Studio:** open `RaceDay/RaceDay.sln`, right-click the solution, choose *Configure Startup Projects -> Multiple startup projects* and set **RaceDay.Api** and **RaceDay.Web** to *Start*, then press F5.
4. **Image uploads (optional):** set `BlobStorage:ConnectionString` in the API's `appsettings.json` (user secrets recommended) - either a real Azure Storage connection string, or `UseDevelopmentStorage=true` with Azurite running (`npx azurite-blob`). Without it everything works except uploads, which show a friendly "temporarily unavailable" message.

> The API has extra columns added in Part 3 (`EVENTS.EventType`, `EVENTS.DistanceKM`, `EVENTS.BannerUrl`, `USERS.ProfilePictureUrl`). `EnsureCreated()` will not alter an existing database - **delete the old `RaceDayDb`** (or run the updated SQL script) before running.

## Part 3 - how the requirements are met

### 1. MVC application setup
`RaceDay.Web` talks to the API through one typed client (`Services/RaceDayApi.cs`). There is no `DbContext`, no connection string and no Azure SDK in the MVC project - no database access, no business logic and no direct Azure access.

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
Bootstrap 5.3 plus a custom sporting theme (`wwwroot/css/site.css`: navy / electric orange, Barlow Condensed headings). One layout, one stylesheet and shared partials keep fonts, colours, spacing and components identical on every page. Statuses are colour-coded everywhere: **green** Confirmed/Completed, **amber** Registered/Pending, **red** Cancelled/DNS/Disqualified, **orange** DNF. Invalid input shows inline messages, API failures are turned into friendly alerts, and unexpected errors land on a friendly error page - raw errors are never shown.

### 8. GitHub and CI/CD
See [CI/CD](#cicd).

## Part 1 - Design

Part 1 defines what Part 2 implements.

- **ERD:** _add link or image path here_
- **Endpoint plan:** _add link or document path here_
- **SQL schema:** `schema.sql` contains the database schema derived from the ERD (`RaceDay/database/RaceDay_Schema_and_Seed.sql` adds the Part 3 columns and sample data).

## Part 2 - RaceDay API

ASP.NET Core 8 Web API for the RaceDay event management system, built to the Part 1 endpoint plan and ERD.

- **Stack:** ASP.NET Core Web API (C#), Entity Framework Core (Code-First) + SQL Server, JWT authentication, Swagger UI, xUnit tests, GitHub Actions CI.
- **Roles:** `Organiser` and `Participant` (role-based access enforced with `[Authorize(Roles = ...)]` plus ownership checks).

### API roles

| Role | Can do |
|------|--------|
| **Organiser** | Create/update/delete **own** events; manage the category master list; attach categories (fee + capacity) to own events; view enrolments and registrations for own events; assign race numbers; capture results; manage routes and weather for own events. |
| **Participant** | View events, categories, routes, weather and results; enrol in / cancel own enrolments in an event category; view own enrolments, registrations and results. |
| **Public (no login)** | Register, log in, browse events/categories/routes/weather/category results. |

Any endpoint a role is not allowed to use returns **403 Forbidden**; a missing/invalid token returns **401 Unauthorized**.

### API setup and run (without Docker)

1. Open `RaceDay/RaceDay.sln` in Visual Studio (or use the CLI below).
2. Check the connection string in `RaceDay/src/RaceDay.Api/appsettings.json` (`ConnectionStrings:DefaultConnection`). The default uses LocalDB:
   `Server=(localdb)\MSSQLLocalDB;Database=RaceDayDb;Trusted_Connection=True;TrustServerCertificate=True`
3. Run:
   ```bash
   cd RaceDay
   dotnet restore
   dotnet run --project src/RaceDay.Api
   ```
4. Open **http://localhost:5080** - Swagger UI loads at the site root.

The database schema is created automatically on first start (Code-First, matches the ERD) and the master `Categories` list is seeded.

> Prefer migrations? From `RaceDay/`, run `dotnet ef migrations add InitialCreate --project src/RaceDay.Api` and replace `db.Database.EnsureCreated()` in `Program.cs` with `db.Database.Migrate()`.

#### Using Swagger with authentication

1. `POST /api/auth/register` (choose role `Organiser` or `Participant`).
2. `POST /api/auth/login` and copy the `token` from the response.
3. Click **Authorize** (top right), paste the token, and test the protected endpoints.

### Endpoints (from the Part 1 plan)

Authentication, User Profile, Events, Categories, Event Categories, Enrollments, User Events (registrations), Results, Routes and Weather Information - all endpoints in the plan are implemented and visible in Swagger with descriptions, request bodies and responses. Part 3 added `GET /api/events/mine` (Organiser) so the MVC app can list the logged-in organiser's own events, plus the two image upload endpoints.

## Running the unit tests

From `RaceDay/`:

```bash
dotnet test
```

Tests use the real API in memory (`WebApplicationFactory`) with an isolated EF Core in-memory database (blob storage is replaced by an in-memory fake). They cover authenticated and unauthenticated requests, success and failure cases, and role enforcement for: registration/login, user profile, events, categories, event categories, enrolments, registrations, results, routes and weather, image uploads, plus Swagger generation.

## CI/CD

`.github/workflows/ci.yml` runs on every push and pull request (and can be started manually from the Actions tab). It has two jobs:

1. **build-and-test** - restores, builds (Release) the **whole solution (API, MVC project and tests)** and runs all tests on .NET 8.
2. **docker-build** - builds both Docker images (API and MVC), only if job 1 passes.

**CI screenshot (green build):** <img width="701" height="253" alt="Screenshot 2026-11-09 145307" src="https://github.com/user-attachments/assets/278ef6b4-bfde-4c86-8571-6d53a665ac6a" />


## Repository structure

```
README.md
schema.sql                          Part 1 - SQL schema
.github/workflows/ci.yml            CI workflow (build, test, Docker images)
RaceDay/
  RaceDay.sln
  docker-compose.yml                SQL Server + Azurite + API + MVC
  database/RaceDay_Schema_and_Seed.sql
  src/RaceDay.Api/                  Part 2 API: Controllers, Models, Data, Dtos, Services (JWT, passwords, Blob Storage), Dockerfile
  src/RaceDay.Web/                  Part 3 MVC: Controllers, Models, Services (API client, session), Filters, Views, wwwroot, Dockerfile
  tests/RaceDay.Tests/              xUnit tests
```

## Notes / design decisions

- Passwords are hashed with PBKDF2 (random salt) and never stored in plain text.
- Authentication uses JWT bearer tokens (as specified in the endpoint plan); the role is carried in the token and checked on every request. The MVC app keeps the JWT in the server-side session, so it is never exposed to the browser.
- Enrolling creates the participant's event registration (`UserEvents`); cancelling the last enrolment for an event removes it.
- The MVC app has no database or Azure access; everything goes through the API.
- AI assistance was used to help generate this project; review and understand the code before submitting.
