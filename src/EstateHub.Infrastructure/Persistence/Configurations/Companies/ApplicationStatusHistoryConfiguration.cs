using EstateHub.Domain.Entities.Companies;
using EstateHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Companies;

public sealed class ApplicationStatusHistoryConfiguration : IEntityTypeConfiguration<ApplicationStatusHistory>
{
    public void Configure(EntityTypeBuilder<ApplicationStatusHistory> builder)
    {
        builder.HasKey(history => history.Id);

        builder.Property(history => history.ToStatus)
            .IsRequired();

        builder.Property(history => history.Reason)
            .HasMaxLength(1000);

        builder.HasOne(history => history.CompanyApplication)
            .WithMany(application => application.StatusHistory)
            .HasForeignKey(history => history.CompanyApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(history => history.ChangedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
