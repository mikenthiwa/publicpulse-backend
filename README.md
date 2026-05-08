# PublicPulse Backend

.NET Web API backend for PublicPulse, a civic-tech platform for reporting damaged roads and public infrastructure issues.

## Tech Stack

- .NET 10 Web API
- PostgreSQL configuration through environment variables
- Swagger/OpenAPI

## Getting Started

Restore dependencies:

```bash
dotnet restore
```

Run the API:

```bash
ASPNETCORE_URLS=http://localhost:5000 dotnet run
```

Health check:

```bash
curl http://localhost:5000/health
```

Swagger UI is available at `http://localhost:5000/swagger` in development.

## Environment Variables

Copy `.env.example` into your local environment manager or export the values in your shell.

| Variable | Description |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment, usually `Development` locally |
| `ASPNETCORE_URLS` | Local URL binding for the API |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |

## Project Structure

- `Program.cs` - API startup and minimal endpoints
- `Modules/Issues` - Placeholder for issue and report features
- `Modules/Users` - Placeholder for user features
- `Modules/Comments` - Placeholder for comment features
- `Modules/InfrastructureCategories` - Placeholder for category features
