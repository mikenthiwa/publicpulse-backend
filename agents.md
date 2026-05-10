# Setup

- Use the .NET SDK that supports `net10.0`; this project targets `.NET 10`.
- Configure local settings from `.env.example`, including `ConnectionStrings__DefaultConnection`.
- Swagger is available at `http://localhost:5000/swagger` in Development.

# Build and Development Commands

- Restore dependencies: `dotnet restore PublicPulse.Backend.sln`.
- Build the solution: `dotnet build PublicPulse.Backend.sln`.
- Run the API: `ASPNETCORE_URLS=http://localhost:5000 dotnet run --project src/Web/Web.csproj`.
- Verify the health endpoint: `curl http://localhost:5000/health`.

# Testing

- No test project exists yet.
- For current verification, run `dotnet build PublicPulse.Backend.sln`.
- When the API is running, verify behavior with `curl http://localhost:5000/health`.
- Add or update tests when introducing meaningful behavior changes.
- If tests are added later, include them in `PublicPulse.Backend.sln` and document the command here.

# Style

- Preserve existing .NET Web API and minimal endpoint conventions.
- Follow `.editorconfig`: UTF-8, LF endings, final newline, spaces, and 4-space default indentation.
- Use 2-space indentation for JSON, Markdown, YAML, and YML files.
- Keep nullable reference types and implicit usings enabled for C# code.
- Prefer clear records, explicit names, and small focused handlers over clever shortcuts.

# Review

- Keep changes focused on the requested behavior.
- Show diffs for large or multi-file changes before applying them.
- Check for behavior regressions, missing tests, unnecessary complexity, and convention drift.
- Run `dotnet build PublicPulse.Backend.sln` before marking work complete.
- Call out skipped checks, assumptions, risks, and follow-up work when relevant.

# Commit and Pull Request Guidelines

- Use focused, descriptive commits with conventional prefixes such as `feat:`, `refactor:`, `chore` and `test:`.
- Keep each commit scoped to one coherent change.
- PRs should include a short summary of the change and affected layers.
- Link the issue or task reference in the PR.
- Include test evidence: `dotnet test`, a targeted test command, or API verification.
- Note config changes, migrations, or new secrets when applicable.

# Security and Configuration Tips

- Use `.env.example` as the local configuration reference.
- Do not commit secrets, real connection strings, or machine-specific settings.
- Document any new environment variables in `.env.example` and the README.
- Call out config, migration, or secret impacts during review.
