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

## Project Structure

- `PublicPulse.Backend.sln` - Solution file
- `src/Web/Program.cs` - API startup and minimal endpoints
- `src/Web/Infrastructure/Persistence` - EF Core DbContext and migrations
- `src/Web/Features/Issues` - Placeholder for issue and report features
- `src/Web/Features/Users` - Placeholder for user features
- `src/Web/Features/Comments` - Placeholder for comment features
- `src/Web/Features/InfrastructureCategories` - Placeholder for category features
- `tests/Web.UnitTests` - Unit tests for records, helpers, and isolated behavior
- `tests/Web.IntegrationTests` - Integration tests for HTTP endpoints and API middleware
