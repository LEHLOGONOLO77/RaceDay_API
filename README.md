# RaceDay API (Part 2)

RESTful API for the RaceDay system, built with ASP.NET Core Web API (.NET 8), Entity Framework Core (Code-First) and SQL Server, with cookie-based session authentication and role-based access for **Organisers** and **Participants**.

## CI/CD status

> Replace this with your own screenshot of a green GitHub Actions run:
>
> `![Green build](docs/green-build.png)`

## Demo video

> Replace with your unlisted YouTube link: `https://youtu.be/XXXXXXXXXXX`

## Tech stack

- ASP.NET Core Web API (controllers), .NET 8
- Entity Framework Core 8 (Code-First, migrations) + SQL Server (LocalDB by default)
- Cookie authentication (session), ASP.NET Core `PasswordHasher` (PBKDF2) for password hashing
- Swagger / Swashbuckle with XML documentation
- xUnit + `WebApplicationFactory` integration tests (in-memory database)
- GitHub Actions (build + test on every push)

## Setup and run

Prerequisites: .NET 8 SDK, SQL Server or SQL Server LocalDB (installed with Visual Studio), Git.

```bash
git clone <your-repo-url>
cd <repo-folder>

# 1. Check the connection string in RaceDay.Api/appsettings.json (defaults to LocalDB)

# 2. Create the database schema from the EF Core migrations
dotnet tool install --global dotnet-ef        # once only
cd RaceDay.Api
dotnet ef migrations add InitialCreate        # first time only, if no Migrations folder exists
dotnet ef database update

# 3. Run the API
dotnet run --launch-profile https
```

Open **https://localhost:7100/swagger** to view and test every endpoint. In Development the app also applies pending migrations at startup.

### Using the API in Swagger

1. `POST /api/auth/register` to create an Organiser (e.g. `"role": "Organiser"`).
2. `POST /api/auth/login`. The browser stores the `RaceDay.Session` cookie and sends it automatically on later requests.
3. Use the protected endpoints. To switch role, call `POST /api/auth/logout` then log in as the other user (or use a private window).

## Running the tests

```bash
dotnet test
```

Tests boot the real API in memory with an in-memory database, so no SQL Server is needed. They cover registration and login, event management by organisers, role rejection (401 vs 403), ownership rules, registrations (enrolments), bib numbers and results.

## Roles

| Role | Can do |
|---|---|
| **Organiser** | Create, update and delete **their own** events, routes and categories; view registrations for their events; mark payment, assign bib numbers; record, update and delete results; manage their own profile. |
| **Participant** | Register an account, manage their own profile, view events/routes/categories/results; register for an event category; pay/cancel their own registration; view their own registrations and results. |

Role is chosen at registration and validated against the allowed values (`Organiser`, `Participant`). It is stored with the account and placed in the session cookie at login. **401** means not logged in; **403** means logged in but not allowed (wrong role, or someone else's data).

Ownership rules go beyond role checks: an organiser can only change events they created, and a participant can only see their own registrations and results.

## Endpoints

All endpoints are documented in Swagger. Summary:

| Area | Endpoints |
|---|---|
| Auth | `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me` |
| Events | `GET /api/events`, `GET /api/events/{id}`, `POST`, `PUT`, `DELETE /api/events/{id}`, `GET /api/organisers/{id}/events` |
| Routes | `POST /api/routes`, `GET /api/events/{id}/routes`, `PUT`/`DELETE /api/routes/{id}` |
| Categories | `POST /api/event-categories`, `GET /api/events/{id}/categories`, `PUT`/`DELETE /api/event-categories/{id}` |
| Participants | `POST /api/participants`, `GET`/`PUT`/`DELETE /api/participants/{id}` |
| Registrations | `POST /api/registrations`, `GET`/`PUT`/`DELETE /api/registrations/{id}`, `GET /api/participants/{id}/registrations`, `PUT /api/registrations/{id}/payment`, `PUT /api/registrations/{id}/bib-number` |
| Results | `POST /api/results`, `GET /api/events/{id}/results`, `GET /api/participants/{id}/results`, `PUT`/`DELETE /api/results/{id}` |

## Changes from the Part 1 plan

These were needed to meet the Part 2 brief or to make the system safe. Update this list to match your own decisions.

1. **Added authentication endpoints** (`/api/auth/*`): the brief requires role-based registration and login, which the Part 1 plan did not cover.
2. **Added organiser profile endpoints** (`GET`/`PUT /api/organisers/{id}`): the brief requires both roles to view and update their own profile.
3. **Added `GET /api/events/{id}/registrations`** (and `GET /api/registrations/{id}`): the brief requires organisers to view all enrolments for their events.
4. **Event has `name` and `distance`**: required by the brief.
5. **`POST /api/participants`** also requires a `password`, since the participant must be able to log in.
6. **`registrationDate` is set by the server** and is not accepted in the request body of `POST /api/registrations`.
7. **Ownership checks** were added on top of role checks (see Roles).
8. **Registration rules:** only events with status `Open` accept registrations; cancelling keeps the record with status `Cancelled`; participants can mark only their own registration `Paid` or `Cancelled`; bib numbers go only to paid, non-cancelled registrations and must be unique within an event.
9. **Result rules:** the participant must hold a non-cancelled registration in the given category; one result per participant per event; positions are unique per category.
10. **Delete rules:** events and categories with registrations or results return `409 Conflict`. Deleting a participant account also removes their registrations and results.

## Data model

`User` (login + role) one-to-one with `Organiser` or `Participant` profile. `Organiser` has many `Event`; `Event` has many `EventRoute` and `EventCategory`; `Participant` and `EventCategory` are linked by `Registration`; `Result` records position and finish time per participant per event. Passwords are stored only as hashes.
