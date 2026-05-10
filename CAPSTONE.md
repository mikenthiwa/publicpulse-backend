# Capstone spec — PublicPulse Backend

## Problem statement
PublicPulse Backend provides the API and data layer for a civic-tech platform that helps citizens report damaged roads and public infrastructure issues. It allows reports to be created, stored, categorized, tracked, and updated so that citizen complaints become structured, visible, and actionable instead of being scattered across informal channels.

## What success looks like

- [ ] A citizen can create an infrastructure report with title, description, category, photo URL, and location.
- [ ] Users can retrieve a public list of reported issues.
- [ ] Users can view the full details of a single report.
- [ ] Citizens can confirm/upvote an existing issue.
- [ ] A report status can be updated from `Reported` to `In Progress` to `Resolved`.
- [ ] API endpoints are documented in Swagger.
- [ ] Backend tests run successfully using `dotnet test`.

## Architecture sketch

- A .NET Web API exposing REST endpoints for reports, categories, comments, confirmations, and status updates.
- PostgreSQL database accessed through EF Core.
- Service/repository layer to keep business logic separate from controllers.
- Swagger/OpenAPI for API documentation and testing.

## Tech stack

- Language: C#
- Framework: .NET Web API
- Database: PostgreSQL
- ORM: Entity Framework Core
- API docs: Swagger/OpenAPI
- Testing: xUnit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing
- Configuration: Environment variables and `.env.example`

## Task list

1. [ ] Confirm the backend project runs locally.
2. [ ] Add PostgreSQL and EF Core configuration.
3. [ ] Create core entities: `Report`, `Category`, `Comment`, `ReportConfirmation`, and `User`.
4. [ ] Create enums for report status and priority if needed.
5. [ ] Add initial EF Core migration.
6. [ ] Seed basic report categories.
7. [ ] Implement report creation endpoint.
8. [ ] Implement endpoint to list reports.
9. [ ] Implement endpoint to get report by ID.
10. [ ] Implement endpoint to confirm/upvote a report.
11. [ ] Implement endpoint to update report status.
12. [ ] Add request/response DTOs.
13. [ ] Add validation for required fields.
14. [ ] Add consistent API response format.
15. [ ] Add unit tests for services/validation.
16. [ ] Add integration tests for core endpoints.
17. [ ] Update README with backend setup, environment variables, and test commands.

## Out of scope for backend MVP

- Authentication and role-based access control.
- AI image analysis.
- Real-time notifications.
- Advanced admin dashboard.
- SMS/USSD integration.
- Payment or donation features.
- Complex geospatial analytics.
- Production deployment setup.

## Open questions

- Should users submit reports anonymously or with accounts?
- Should photo upload be handled by the backend or by an external service?
- Should location use latitude/longitude only, or also county/ward/road name?
- Who is allowed to update report status in the MVP?
- Should duplicate reports near the same location be merged?
