using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Domain.Entities;
using Web.Domain.Enums;

namespace Web.Infrastructure.Persistence;

public static class InitialiserExtensions
{
    public static async Task InitialiseAsync(this WebApplication application)
    {
        var scope = application.Services.CreateScope();
        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser(
    ApplicationDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    ILogger<ApplicationDbContextInitialiser> logger)
{
    private static readonly Guid DemoUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid AdminUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid RiversideDriveReportId = Guid.Parse("c1111111-1111-1111-1111-111111111111");
    private static readonly Guid WaiyakiWayReportId = Guid.Parse("c2222222-2222-2222-2222-222222222222");
    private static readonly Guid OgingaOdingaReportId = Guid.Parse("c3333333-3333-3333-3333-333333333333");
    private static readonly Guid NyaliBridgeReportId = Guid.Parse("c4444444-4444-4444-4444-444444444444");
    private const string DemoPassword = "Password123!";

    public async Task InitialiseAsync()
    {
        try
        {
            // See https://jasontaylor.dev/ef-core-database-initialisation-strategies
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while seeding the database.");
            throw;
        }
    }
    
    private async Task TrySeedAsync()
    {
        if (!await dbContext.Users.AnyAsync())
        {
            var demoUser = CreateUser(DemoUserId, "demo@publicpulse.local");
            var adminUser = CreateUser(AdminUserId, "admin@publicpulse.local");

            dbContext.Users.AddRange(demoUser, adminUser);
            await dbContext.SaveChangesAsync();
        }

        if (await dbContext.Reports.AnyAsync())
        {
            return;
        }

        var seededUsers = await dbContext.Users
            .Where(user => user.Id == DemoUserId || user.Id == AdminUserId)
            .ToDictionaryAsync(user => user.Id);

        if (!seededUsers.TryGetValue(DemoUserId, out var demoUserForReports)
            || !seededUsers.TryGetValue(AdminUserId, out var adminUserForReports))
        {
            logger.LogWarning("Skipping report seed data because expected demo users were not found.");
            return;
        }

        var reports = new[]
        {
            new Report
            {
                Id = RiversideDriveReportId,
                Description = "Large pothole forming near the bus stop and forcing drivers into the opposite lane.",
                CategoryId = Category.RoadsId,
                County = "Nairobi",
                RoadName = "Riverside Drive",
                Latitude = -1.267569,
                Longitude = 36.802119,
                LocationLabel = "Riverside Drive near Chiromo Lane",
                LocationSource = "Seed data",
                Status = ReportStatus.Reported,
                Created = DateTimeOffset.UtcNow.AddDays(-5),
                CreatedBy = demoUserForReports.Id,
                CreatedByUser = demoUserForReports
            },
            new Report
            {
                Id = WaiyakiWayReportId,
                Description = "Blocked storm drain causing water to pool across the service lane after rain.",
                CategoryId = Category.DrainageId,
                County = "Nairobi",
                RoadName = "Waiyaki Way",
                Latitude = -1.262053,
                Longitude = 36.766641,
                LocationLabel = "Waiyaki Way service lane, Westlands",
                LocationSource = "Seed data",
                Status = ReportStatus.InProgress,
                Created = DateTimeOffset.UtcNow.AddDays(-3),
                LastModified = DateTimeOffset.UtcNow.AddDays(-1),
                CreatedBy = demoUserForReports.Id,
                CreatedByUser = demoUserForReports
            },
            new Report
            {
                Id = OgingaOdingaReportId,
                Description = "Street lights are out on a busy pedestrian section, reducing visibility at night.",
                CategoryId = Category.StreetLightsId,
                County = "Kisumu",
                RoadName = "Oginga Odinga Road",
                Latitude = -0.102195,
                Longitude = 34.752158,
                LocationLabel = "Oginga Odinga Road near the central market",
                LocationSource = "Seed data",
                Status = ReportStatus.Resolved,
                Created = DateTimeOffset.UtcNow.AddDays(-12),
                LastModified = DateTimeOffset.UtcNow.AddDays(-2),
                CreatedBy = adminUserForReports.Id,
                CreatedByUser = adminUserForReports
            },
            new Report
            {
                Id = NyaliBridgeReportId,
                Description = "Damaged guardrail section on the approach to the bridge needs inspection.",
                CategoryId = Category.BridgesId,
                County = "Mombasa",
                RoadName = "Nyali Bridge Approach",
                Latitude = -4.044881,
                Longitude = 39.680054,
                LocationLabel = "Nyali Bridge southbound approach",
                LocationSource = "Seed data",
                Status = ReportStatus.Reported,
                Created = DateTimeOffset.UtcNow.AddDays(-1),
                CreatedBy = adminUserForReports.Id,
                CreatedByUser = adminUserForReports
            }
        };

        reports[0].Images.Add(new ReportImage
        {
            ReportId = reports[0].Id,
            ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1717000000/public-pulse/seeds/riverside-pothole.jpg",
            PublicId = "public-pulse/seeds/riverside-pothole",
            Created = reports[0].Created,
            CreatedBy = reports[0].CreatedBy
        });

        reports[1].Images.Add(new ReportImage
        {
            ReportId = reports[1].Id,
            ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1717000001/public-pulse/seeds/waiyaki-drainage.jpg",
            PublicId = "public-pulse/seeds/waiyaki-drainage",
            Created = reports[1].Created,
            CreatedBy = reports[1].CreatedBy
        });

        reports[2].Images.Add(new ReportImage
        {
            ReportId = reports[2].Id,
            ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1717000002/public-pulse/seeds/kisumu-streetlight.jpg",
            PublicId = "public-pulse/seeds/kisumu-streetlight",
            Created = reports[2].Created,
            CreatedBy = reports[2].CreatedBy
        });

        reports[0].Confirmations.Add(new ReportConfirmation
        {
            ReportId = reports[0].Id,
            Created = reports[0].Created.AddHours(6),
            CreatedBy = AdminUserId
        });

        reports[1].Confirmations.Add(new ReportConfirmation
        {
            ReportId = reports[1].Id,
            Created = reports[1].Created.AddHours(2),
            CreatedBy = AdminUserId
        });

        reports[1].Confirmations.Add(new ReportConfirmation
        {
            ReportId = reports[1].Id,
            Created = reports[1].Created.AddHours(8),
            CreatedBy = DemoUserId
        });

        reports[2].Confirmations.Add(new ReportConfirmation
        {
            ReportId = reports[2].Id,
            Created = reports[2].Created.AddDays(1),
            CreatedBy = DemoUserId
        });

        dbContext.Reports.AddRange(reports);
        await dbContext.SaveChangesAsync();
    }

    private User CreateUser(Guid id, string email)
    {
        var user = new User
        {
            Id = id,
            Email = email,
            PasswordHash = string.Empty,
            Created = DateTimeOffset.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, DemoPassword);

        return user;
    }
}
