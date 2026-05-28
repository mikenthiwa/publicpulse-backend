# PublicPulse Backend

.NET Web API backend for PublicPulse, a civic-tech platform for reporting damaged roads and public infrastructure issues.

## Tech Stack

- .NET 10 Web API
- PostgreSQL configuration through user secrets or environment variables
- Swagger/OpenAPI

## Getting Started

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

## PostgreSQL

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

The `/health` endpoint checks PostgreSQL connectivity and returns `503 Service Unavailable` when the database cannot be reached.

## Authentication

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

## MVP Endpoints

- `GET /api/Categories` - list seeded report categories.
- `GET /api/Locations/reverse?latitude={lat}&longitude={lng}` - suggest county and road name from browser GPS coordinates.
- `POST /api/Reports` - create a report, authenticated.
- `GET /api/Reports` - list public reports.
- `GET /api/Reports/{id}` - get public report details.
- `POST /api/Reports/{id}/confirmations` - anonymously confirm/upvote a report.
- `PUT /api/Reports/{id}/status` - update report status, authenticated creator only.

Report responses are public and do not expose creator identity.

Location lookup is optional assistance for the frontend. Users can still manually enter county and road name if browser geolocation is unavailable, permission is denied, or reverse geocoding fails.

## Testing

The solution has two test projects:

- `tests/Web.UnitTests` - fast tests for records, helpers, and isolated behavior.
- `tests/Web.IntegrationTests` - HTTP-level tests using `WebApplicationFactory`, without a real PostgreSQL dependency.

Run all tests with:

```bash
dotnet test PublicPulse.Backend.sln
```

## Environment Variables

Copy `.env.example` into your local environment manager or export the values in your shell.

| Variable | Description |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment, usually `Development` locally |
| `ASPNETCORE_URLS` | Local URL binding for the API |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
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

## Project Structure

- `PublicPulse.Backend.sln` - Solution file
- `src/Web/Program.cs` - API startup and minimal endpoints
- `src/Web/Infrastructure/Persistence` - EF Core DbContext and migrations
- `src/Web/Features/Auth` - Registration, login, and JWT token services
- `src/Web/Features/Categories` - Report category model and contracts
- `src/Web/Features/Reports` - Reports, confirmations, status, and report service
- `tests/Web.UnitTests` - Unit tests for records, helpers, and isolated behavior
- `tests/Web.IntegrationTests` - Integration tests for HTTP endpoints and API middleware
