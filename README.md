# PublicPulse Backend

An API for reporting damaged roads and public infrastructure issues.

## Description

**PublicPulse Backend** is the .NET Web API for PublicPulse. It supports user authentication, infrastructure reports, report images, confirmations, and location lookup, with PostgreSQL persistence.

## Table of Contents

- [Documentation](#documentation)
- [Setup](#setup)
  - [Dependencies](#dependencies)
  - [Getting Started](#getting-started)
  - [Environment Variables](#environment-variables)
  - [Database and ORM](#database-and-orm)
  - [Run the Service Using Docker](#run-the-service-using-docker)
- [Testing](#testing)
- [Contribute](#contribute)
- [Deployment](#deployment)
- [License](#license)
- [Project Structure](#project-structure)

## Documentation

- [Development API documentation (Swagger UI)](https://publicpulse-api-dev-gcgvdha4h0b0hha9.westus3-01.azurewebsites.net/swagger/index.html)
- [Local API documentation (Swagger UI)](http://localhost:5000/swagger), available when running locally in Development.

See [CAPSTONE.md](CAPSTONE.md) for project context.

### Authentication

The MVP uses local email/password accounts with JWT bearer tokens.

Required JWT settings:

| Setting | Description |
| --- | --- |
| `Jwt:Issuer` | Token issuer |
| `Jwt:Audience` | Token audience |
| `Jwt:SigningKey` | Long signing key used to sign tokens |
| `Jwt:ExpiryMinutes` | Token lifetime in minutes |

Register and login:

```http
POST /api/Auth/register
POST /api/Auth/login
```

Use the returned token as a bearer token for authenticated endpoints:

```http
Authorization: Bearer <token>
```

### MVP Endpoints

- `GET /api/Categories` - list seeded report categories.
- `GET /api/Locations/reverse?latitude={lat}&longitude={lng}` - suggest county and road name from browser GPS coordinates.
- `POST /api/Reports` - create a report, authenticated.
- `GET /api/Reports` - list public reports.
- `GET /api/Reports/{id}` - get public report details.
- `POST /api/Reports/{id}/confirmations` - anonymously confirm/upvote a report.
- `PUT /api/Reports/{id}/status` - update report status, authenticated creator only.

Report responses are public and do not expose creator identity.

Location lookup is optional assistance for the frontend. Users can still manually enter county and road name if browser geolocation is unavailable, permission is denied, or reverse geocoding fails.

## Setup

### Dependencies

- .NET 10 SDK for building and running the API locally.
- PostgreSQL for application data and Entity Framework Core for migrations.
- Docker with Docker Compose for the container workflow.
- Azure CLI for authenticating to Azure Container Registry during deployment.
- Swagger/OpenAPI for exploring the API in Development.

### Getting Started

Restore dependencies:

```bash
dotnet restore PublicPulse.Backend.sln
```

Build the solution:

```bash
dotnet build PublicPulse.Backend.sln
```

Run tests:

```bash
dotnet test PublicPulse.Backend.sln
```

Create a local PostgreSQL database:

```bash
createdb publicpulse
```

Configure the local PostgreSQL connection string with .NET user secrets:

```bash
dotnet user-secrets init --project src/Web/Web.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=publicpulse;Username=postgres;Password=postgres" --project src/Web/Web.csproj
dotnet user-secrets set "Jwt:Issuer" "PublicPulse" --project src/Web/Web.csproj
dotnet user-secrets set "Jwt:Audience" "PublicPulse" --project src/Web/Web.csproj
dotnet user-secrets set "Jwt:SigningKey" "replace-with-a-long-random-development-signing-key" --project src/Web/Web.csproj
dotnet user-secrets set "Jwt:ExpiryMinutes" "60" --project src/Web/Web.csproj
dotnet user-secrets set "Mapbox:AccessToken" "<mapbox-access-token>" --project src/Web/Web.csproj
```

Run the API:

```bash
ASPNETCORE_URLS=http://localhost:5000 dotnet run --project src/Web/Web.csproj
```

Health check:

```bash
curl http://localhost:5000/health
```

Swagger UI is available at `http://localhost:5000/swagger` in development.

### Environment Variables

Copy `.env.example` into your local environment manager or export the values in your shell.

| Variable | Description |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment, usually `Development` locally |
| `ASPNETCORE_URLS` | Local URL binding for the API |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` | Compose PostgreSQL database and credentials |
| `API_PORT`, `POSTGRES_PORT` | Optional host ports used by Compose |
| `Jwt__Issuer` | JWT token issuer |
| `Jwt__Audience` | JWT token audience |
| `Jwt__SigningKey` | JWT signing key |
| `Jwt__ExpiryMinutes` | JWT token lifetime in minutes |
| `Cloudinary__CloudName` | Cloudinary cloud name, stored with user secrets locally |
| `Cloudinary__ApiKey` | Cloudinary API key, stored with user secrets locally |
| `Cloudinary__ApiSecret` | Cloudinary API secret, stored with user secrets locally |
| `Cloudinary__Folder` | Cloudinary folder prefix for report images |
| `Cloudinary__UploadPreset` | Cloudinary upload preset that enforces allowed image formats and file size |
| `Cloudinary__MaxImagesPerReport` | Maximum number of images per report |
| `Mapbox__AccessToken` or `MAPBOX_ACCESS_TOKEN` | Server-side Mapbox token for reverse geocoding; never expose this to clients |
| `SeedData__Enabled` | Enables idempotent demo data only in Development; defaults to `false` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Optional Application Insights connection string; leave blank to disable Azure Monitor export |
| `OTEL_SERVICE_NAME` | Stable service name shown in Application Insights; defaults to `publicpulse-api` |
| `OTEL_TRACES_SAMPLER` | OpenTelemetry trace sampler; production uses `microsoft.rate_limited` |
| `OTEL_TRACES_SAMPLER_ARG` | Rate limit in traces per second; starts at `0.1` |

Compose maps the uppercase variables in `.env.example` (for example
`JWT_SIGNING_KEY`) to their ASP.NET configuration names. The duplicate
double-underscore variables at the bottom of the example are provided for
direct `dotnet run` workflows.

Store Cloudinary account credentials with .NET user secrets for local development:

```bash
dotnet user-secrets set "Cloudinary:CloudName" "<cloud-name>" --project src/Web/Web.csproj
dotnet user-secrets set "Cloudinary:ApiKey" "<api-key>" --project src/Web/Web.csproj
dotnet user-secrets set "Cloudinary:ApiSecret" "<api-secret>" --project src/Web/Web.csproj
```

Store the Mapbox access token with .NET user secrets for local development:

```bash
dotnet user-secrets set "Mapbox:AccessToken" "<mapbox-access-token>" --project src/Web/Web.csproj
```

Report images use browser-direct Cloudinary uploads:

1. Call authenticated `POST /api/Reports/images/upload-signature`.
2. Upload the image directly from the browser to `https://api.cloudinary.com/v1_1/{cloudName}/image/upload` with `api_key`, `timestamp`, `folder`, `upload_preset`, `signature`, and `file`.
3. Send JSON to `POST /api/Reports` with `Description`, `CategoryId`, `County`, `RoadName`, and one to five `Images` entries containing `publicId`, `version`, and `signature` from Cloudinary. Optional `Latitude`, `Longitude`, `LocationLabel`, and `LocationSource` fields can be included when assisted location lookup succeeds. The API derives and stores the final Cloudinary image URL.

Configure the Cloudinary upload preset to allow only report image formats and enforce the desired per-file size limit.

### Database and ORM

The API requires a PostgreSQL connection string named `DefaultConnection`.

Example:

```text
Host=localhost;Port=5432;Database=publicpulse;Username=postgres;Password=postgres
```

For local development, prefer .NET user secrets:

```bash
dotnet user-secrets init --project src/Web/Web.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=publicpulse;Username=postgres;Password=postgres" --project src/Web/Web.csproj
```

Environment variables are also supported:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=publicpulse;Username=postgres;Password=postgres"
```

Do not commit real connection strings or database passwords in appsettings files.

Create a migration:

```bash
dotnet ef migrations add InitialCreate --project src/Web/Web.csproj --startup-project src/Web/Web.csproj --output-dir Infrastructure/Persistence/Migrations
```

Apply migrations:

```bash
dotnet ef database update --project src/Web/Web.csproj --startup-project src/Web/Web.csproj
```

Schema migrations are never applied by normal API startup. In Compose, the
one-shot `migrate` service owns this responsibility. Outside Compose, apply
migrations with the command above before starting the API.

The `/health` endpoint checks PostgreSQL connectivity and returns `503 Service Unavailable` when the database cannot be reached.

### Run the Service Using Docker

Copy the example environment file and replace the placeholder credentials:

```bash
cp .env.example .env
```

Start the production-like stack:

```bash
docker compose up --build --wait
```

The stack contains three services:

- `db` runs PostgreSQL with data stored in the `postgres-data` named volume.
- `migrate` applies the checked-in EF Core migrations and exits successfully.
- `api` starts only after the database is healthy and migrations have completed.

The API is available at `http://localhost:5000`. Its health endpoints are:

```bash
curl http://localhost:5000/health/live
curl http://localhost:5000/health
```

`/health/live` checks that the API process is running. `/health` also checks
PostgreSQL connectivity and returns `503 Service Unavailable` when the database
is unavailable.

For local development, enable Swagger, demo data, and host access to PostgreSQL
with the development override:

```bash
docker compose -f compose.yaml -f compose.dev.yaml up --build --wait
```

Swagger UI is then available at `http://localhost:5000/swagger`. Demo data is
idempotently seeded after migrations when `SEED_DATA_ENABLED=true`. Normal API
startup never deletes or creates the schema.

Useful lifecycle commands:

```bash
docker compose logs -f api
docker compose down
docker compose down -v
```

`docker compose down` preserves PostgreSQL data. `docker compose down -v`
permanently removes the database volume and should only be used when a full
local reset is intended.

The Compose file is suitable for local and single-host deployments. For
production, inject the database connection string, JWT signing key, Cloudinary
credentials, and Mapbox token from the deployment platform's secret store.
Do not use committed values, Docker build arguments, or a checked-in `.env`
file for secrets. Run the `migrator` image once per release before starting or
scaling API replicas, terminate TLS at a reverse proxy or load balancer, avoid
publishing PostgreSQL, and deploy immutable image tags.

## Testing

The solution has two test projects:

- `tests/Web.UnitTests` - fast tests for records, helpers, and isolated behavior.
- `tests/Web.IntegrationTests` - HTTP-level tests using `WebApplicationFactory`, without a real PostgreSQL dependency.

Run all tests with:

```bash
dotnet test PublicPulse.Backend.sln
```

## Contribute

1. Keep changes focused and follow [AGENTS.md](AGENTS.md) and `.editorconfig`.
2. Add or update tests for meaningful behavior changes.
3. Run `dotnet build PublicPulse.Backend.sln` and `dotnet test PublicPulse.Backend.sln`.
4. Use descriptive commits with conventional prefixes such as `feat:`, `refactor:`, `chore:`, or `test:`.
5. Include the affected layers, task reference, and verification results in the pull request. Call out configuration and migration changes; never commit secrets.

## Deployment

Run these commands from the repository root. A code or Dockerfile change follows
this sequence: **build → push → select the new image in App Service → verify**.
An image is a snapshot; editing files does not change an existing image or container.

### Local development versus Azure

To rebuild and recreate the affected local Compose containers:

```bash
docker compose -f compose.yaml -f compose.dev.yaml up --build --wait
```

This uses the local Compose database and migration service. It does not push to
ACR or deploy to Azure. Do not remove the database volume for routine updates.

A standalone `docker build` creates a local Docker image with the exact tag you
specify. It does not update Rider's build output, other image tags, or containers
that are already running. Compilation happens inside Docker.

### Build and push the API image

Replace every `<placeholder>` below with your own value, including the angle
brackets, before running a command. Choose a new, unused `<image-tag>` for each
update and use it consistently throughout build, push, and deployment.

| Placeholder | Meaning |
| --- | --- |
| `<acr-name>` | Azure Container Registry resource name | publicpulsedevacr-addefbhafmc7cwae.azurecr.io
| `<acr-login-server>` | Full registry login hostname from Azure, including `.azurecr.io` |
| `<image-name>` | API image repository name |
| `<image-tag>` | Version label for this release |
| `<app-service-name>` | Azure Web App resource name |
| `<migrator-image-name>` | Separate migration image repository name |
| `<database-host>` | PostgreSQL server hostname |
| `<database-name>` | Target database name |
| `<database-username>` | PostgreSQL login username |
| `<table-name>` | Table to inspect |

The tag does not set `ASPNETCORE_ENVIRONMENT`.

```bash
docker build --platform linux/amd64 --target runtime \
  -t <acr-login-server>/<image-name>:<image-tag> .

# Confirm that the PostgreSQL client is included without starting the API.
docker run --rm --platform linux/amd64 --entrypoint psql \
  <acr-login-server>/<image-name>:<image-tag> --version

az acr login --name <acr-name>

docker push <acr-login-server>/<image-name>:<image-tag>
```

- `--platform linux/amd64` builds for the Azure deployment architecture.
- `--target runtime` selects the API stage, which runs `dotnet Web.dll` on port 8080.
- `-t` builds and tags the image with the full registry name in one command.
- `.` uses the current directory as the build context.

Always specify the target: the Dockerfile's final/default stage is `migrator`,
which runs `./efbundle` and exits. Pushing only uploads an image; it does not build
it, apply migrations, or change App Service's configured image.

If Azure CLI authentication has expired, run `az login`, then `az acr login` again.
Azure CLI login alone does not authenticate Docker to the registry.

If you built with a short local name instead, tag that newly built image before
pushing. For example, only if `<image-name>:<image-tag>` is your updated image:

```bash
docker tag <image-name>:<image-tag> \
  <acr-login-server>/<image-name>:<image-tag>
```

Rebuilding a short tag does not update an existing registry-prefixed tag.
`tag does not exist` means the exact local tag being pushed is missing; repeated
pushes of an old tag will continue uploading the old image.

### Select the image in App Service and verify

1. Open `<app-service-name>` and its Deployment Center/container configuration.
2. Edit the main container: repository `<image-name>`, tag `<image-tag>`, and container port `8080`.
3. Save/apply and wait for the updated container to start. If necessary, restart
   the app. Restarting without changing the configured tag keeps that tag selected.
4. Check the startup logs and request `/health/live` and `/health` on the app's
   current default hostname. Then verify the behavior changed by the release.

`/health/live` checks the running process; `/health` checks database configuration
and connectivity. Neither proves that migrations have been applied.

App Service settings such as `ConnectionStrings__DefaultConnection` and
`Jwt__SigningKey` are injected at runtime. Changing these settings requires saving
and applying them, but does not require rebuilding the image. Use the actual
ASP.NET setting names, including double underscores; Compose's `.env` mappings
are not automatically applied by App Service.

### Apply database migrations separately

For a release with schema changes, build the `migrator` from the same source as
the API and run it against the intended database before deploying API code that
requires the new schema. Review compatibility with any API instances still running.

```bash
docker build --platform linux/amd64 --target migrator \
  -t <migrator-image-name>:<image-tag> .
```

On a Docker host that can reach the target database, securely export
`ConnectionStrings__DefaultConnection` with that database's connection string,
then run the corresponding migration image:

```bash
docker run --rm --platform linux/amd64 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection \
  <migrator-image-name>:<image-tag>
```

`-e ConnectionStrings__DefaultConnection` forwards an already-exported shell
variable; it does not load `.env`, .NET user secrets, or App Service settings.
`Production` selects configuration; the connection string selects the database.
The migrator applies pending migrations and exits. It does not start the API or
invoke demo-data seeding, although data operations inside migrations do execute.

The current Azure PostgreSQL server uses private VNet access. A local Mac cannot
run this migration against it without a network path and working private DNS.
Run it from a host with that access; if the host is remote, transfer or push/pull
the migration image separately. Do not replace App Service's API image with the
one-shot migrator. The runtime image does not contain `efbundle`.

### Inspect Azure tables from the application container

Use App Service's application-container terminal, where shell access is available,
to reach the private database. The runtime image includes `postgresql-client`;
after deploying that image, verify it with `psql --version`. Installing the client
does not itself configure SSH access.

Connect using the current database username and enter the password at the prompt:

```bash
psql "host=<database-host> port=5432 dbname=<database-name> user=<database-username> sslmode=require" -W
```

Inside `psql`, these commands inspect the schema without modifying it:

```sql
-- List tables in the public schema.
\dt public.*

-- Inspect a table's columns (replace the name).
\d public."<table-name>"

-- List applied EF Core migrations, if the history table exists.
SELECT * FROM public."__EFMigrationsHistory" ORDER BY "MigrationId";

-- Disconnect.
\q
```

If `psql` is missing, verify that App Service is running the newly built image.
If the history table is missing, confirm the database and schema before concluding
that migrations have not run. Never paste passwords into documentation or commit them.

## License

No license file is currently included in this repository.

## Project Structure

- `PublicPulse.Backend.sln` - Solution file
- `src/Web/Program.cs` - API startup and minimal endpoints
- `src/Web/Infrastructure/Persistence` - EF Core DbContext and migrations
- `src/Web/Features/Auth` - Registration, login, and JWT token services
- `src/Web/Features/Categories` - Report category model and contracts
- `src/Web/Features/Reports` - Reports, confirmations, status, and report service
- `tests/Web.UnitTests` - Unit tests for records, helpers, and isolated behavior
- `tests/Web.IntegrationTests` - Integration tests for HTTP endpoints and API middleware

## Logging

The API writes JSON logs to the console with UTC timestamps and request scopes. Run
`dotnet run --project src/Web/Web.csproj` and inspect the application output.
Application logs start at Information; ASP.NET Core logs start at Warning.

Request events include method, path (without query string), status and elapsed
milliseconds. Scopes include RequestId, TraceId and ProblemDetailsTraceId; search
ProblemDetailsTraceId using the traceId returned in an error response. ReportCreated
records ReportId and Status immediately after persistence succeeds. RequestCancelled
and RequestFailed distinguish interrupted requests from completed responses.

For example, filter captured console output with
`jq -R 'fromjson? | select(.EventId == 1003)'` for report creation, or
`jq -R 'fromjson? | select(.LogLevel == "Error")'` for errors. Console logs are not
persisted by the application; the hosting environment owns collection and retention.
New request/business events omit headers, query strings, bodies and credentials.

### Azure Monitor and Application Insights

Azure Monitor export is opt-in. The API registers the code-based Azure Monitor
OpenTelemetry distro only when `APPLICATIONINSIGHTS_CONNECTION_STRING` is non-empty.
Without it, the existing JSON console logs continue unchanged and no Azure exporter
is created.

The initial low-volume policy is:

- trace ASP.NET Core requests and outgoing `HttpClient` dependencies;
- exclude `/health` and `/health/live` from request traces;
- export Warning and Error logs by default;
- export the `ReportCreated` Information event;
- suppress routine request-completion Information and Warning logs from Azure export;
- disable Live Metrics; and
- rate-limit traces to `0.1` per second in the deployment environment.

The Azure Monitor distro includes SQL Server instrumentation, not Npgsql
instrumentation. PostgreSQL spans are intentionally deferred until their query
attributes and ingestion volume have been reviewed.

For Azure App Service:

1. Create or reuse a workspace-based Application Insights resource.
2. Keep App Service portal-managed Application Insights instrumentation disabled;
   running it together with the code-based distro can duplicate telemetry.
3. Add `APPLICATIONINSIGHTS_CONNECTION_STRING`, `OTEL_SERVICE_NAME`,
   `OTEL_TRACES_SAMPLER`, and `OTEL_TRACES_SAMPLER_ARG` under App Service
   environment variables.
4. Restart the App Service and make controlled API requests.
5. Confirm data in the `AppRequests`, `AppDependencies`, and `AppTraces` tables.
6. Review ingestion after 24 hours and seven days. Use filtering and sampling as
   primary cost controls, with a low daily cap and cost budget as safeguards.

To disable export without rolling back the application, remove or blank
`APPLICATIONINSIGHTS_CONNECTION_STRING` and restart the App Service.
