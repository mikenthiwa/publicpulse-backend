using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Web.Domain.Entities;

namespace Web.Infrastructure.Persistence.Configuration;

public sealed class ReportConfirmationConfiguration : IEntityTypeConfiguration<ReportConfirmation>
{
    public void Configure(EntityTypeBuilder<ReportConfirmation> builder)
    {
        builder.HasOne(confirmation => confirmation.Report)
            .WithMany(report => report.Confirmations)
            .HasForeignKey(confirmation => confirmation.ReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
