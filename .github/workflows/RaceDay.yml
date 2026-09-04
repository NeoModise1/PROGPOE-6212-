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
