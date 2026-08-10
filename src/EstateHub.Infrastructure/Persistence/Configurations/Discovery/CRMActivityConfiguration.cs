using EstateHub.Domain.Entities.Discovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Discovery;

public sealed class CRMActivityConfiguration : IEntityTypeConfiguration<CRMActivity>
{
    public void Configure(EntityTypeBuilder<CRMActivity> builder)
    {
        builder.HasKey(activity => activity.Id);

        builder.Property(activity => activity.ActivityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(activity => activity.Notes)
            .HasMaxLength(4000);

        builder.HasOne(activity => activity.Lead)
            .WithMany(lead => lead.Activities)
            .HasForeignKey(activity => activity.LeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(activity => activity.PerformedByEmployee)
            .WithMany(employee => employee.PerformedActivities)
            .HasForeignKey(activity => activity.PerformedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
