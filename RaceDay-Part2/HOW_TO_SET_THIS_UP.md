# How to set this up (step by step)

## A. Put the files in your GitHub repo
1. Unzip `RaceDay-Part2.zip`.
2. Copy **everything inside** the `RaceDay-Part2` folder into your local clone of your GitHub repo, so that your repo root contains:
   `.github/`, `Part2-API/`, `README.md`, `.gitignore`.
   (`.github` must be at the repo root or the CI will not run.)
3. If your Part 1 files are in the repo already, keep them – Part 2 lives in `Part2-API/`.

## B. Open and build
1. Open `Part2-API/RaceDay.sln` in Visual Studio 2022.
2. Wait for NuGet restore to finish (needs internet). Build with **Ctrl+Shift+B**.

## C. Create the database (code-first)
In Visual Studio: **Tools > NuGet Package Manager > Package Manager Console**, set *Default project* to `RaceDay.Api`, then:
```
Add-Migration InitialCreate
Update-Database
```
Or from a terminal inside `Part2-API`:
```
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project RaceDay.Api
dotnet ef database update --project RaceDay.Api
```
Commit the generated `Migrations` folder.

If you ran the app BEFORE adding the migration, the database was created without migrations. Drop it and run `Update-Database` again:
`dotnet ef database drop --project RaceDay.Api` then `dotnet ef database update --project RaceDay.Api`.

## D. Align with your Part 1 plan
The brief says the schema must match your Part 1 ERD and SQL script. Compare these with your ERD and rename if needed:
- `Models/Entities.cs` – table/column names (User, Event, Category, Enrolment, RaceResult)
- `Data/RaceDayDbContext.cs` – relationships
- Endpoint routes in `Controllers/` – they must match your approved plan

If you change a model, add a new migration: `Add-Migration <Name>` then `Update-Database`.

## E. Run and test manually
Press **F5**. Swagger opens at `http://localhost:5080/swagger`.
Register an Organiser (code `RACEDAY-ORG-2026`), log in, create an event, add a category; then register a Participant in another browser/incognito window, log in, enrol, and have the Organiser capture the result.

## F. Run unit tests
`dotnet test` in `Part2-API`, or **Test > Run All Tests** in Visual Studio.

## G. GitHub: 20+ commits and CI
1. Create a clearly named branch, e.g. `part2-api`: `git checkout -b part2-api`
2. Commit in small meaningful steps (the brief needs **at least 20 commits from your own account**). For example: project setup, models, DbContext, migration, password hashing, auth controller, session filter, events controller, categories, enrolments, results, Swagger comments, each test file, CI workflow, README.
3. Push: `git push -u origin part2-api`
4. On GitHub open the **Actions** tab, wait for the green build, and take a screenshot.
5. Save it as `docs/ci-green-build.png` (the README already links to it), commit and push.

## H. Finish the README and submit
1. Paste your unlisted YouTube link into `README.md`.
2. Video should show: the running API in Swagger, the code structure, endpoints, authentication (register/login/roles) and the unit tests running.
3. Submit the GitHub repository link on ARC.
