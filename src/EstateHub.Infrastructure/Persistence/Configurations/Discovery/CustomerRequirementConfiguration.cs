using EstateHub.Domain.Entities.Discovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Discovery;

public sealed class CustomerRequirementConfiguration : IEntityTypeConfiguration<CustomerRequirement>
{
    public void Configure(EntityTypeBuilder<CustomerRequirement> builder)
    {
        builder.HasKey(requirement => requirement.Id);

        builder.Property(requirement => requirement.OriginalQuery)
            .HasMaxLength(4000);

        builder.Property(requirement => requirement.StructuredCriteriaJson)
            .IsRequired();

        builder.Property(requirement => requirement.CurrencyCode)
            .HasMaxLength(3);

        builder.Property(requirement => requirement.Notes)
            .HasMaxLength(4000);

        builder.Property(requirement => requirement.MinBudget)
            .HasPrecision(18, 2);

        builder.Property(requirement => requirement.MaxBudget)
            .HasPrecision(18, 2);

        builder.Property(requirement => requirement.MinArea)
            .HasPrecision(18, 2);

        builder.Property(requirement => requirement.MaxArea)
            .HasPrecision(18, 2);

        builder.HasOne(requirement => requirement.Lead)
            .WithMany(lead => lead.Requirements)
            .HasForeignKey(requirement => requirement.LeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(requirement => requirement.Currency)
            .WithMany(currency => currency.CustomerRequirements)
            .HasForeignKey(requirement => requirement.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(requirement => requirement.ConfirmedByEmployee)
            .WithMany(employee => employee.ConfirmedRequirements)
            .HasForeignKey(requirement => requirement.ConfirmedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_CustomerRequirements_BudgetRequiresCurrency",
                "([MinBudget] IS NULL AND [MaxBudget] IS NULL) OR [CurrencyCode] IS NOT NULL");
            table.HasCheckConstraint(
                "CK_CustomerRequirements_MinBudget_NonNegative",
                "[MinBudget] IS NULL OR [MinBudget] >= 0");
            table.HasCheckConstraint(
                "CK_CustomerRequirements_MaxBudget_NonNegative",
                "[MaxBudget] IS NULL OR [MaxBudget] >= 0");
            table.HasCheckConstraint(
                "CK_CustomerRequirements_Budget_Range",
                "[MinBudget] IS NULL OR [MaxBudget] IS NULL OR [MinBudget] <= [MaxBudget]");
            table.HasCheckConstraint(
                "CK_CustomerRequirements_MinBedrooms_NonNegative",
                "[MinBedrooms] IS NULL OR [MinBedrooms] >= 0");
            table.HasCheckConstraint(
                "CK_CustomerRequirements_MinBathrooms_NonNegative",
                "[MinBathrooms] IS NULL OR [MinBathrooms] >= 0");
            table.HasCheckConstraint(
                "CK_CustomerRequirements_MinArea_Positive",
                "[MinArea] IS NULL OR [MinArea] > 0");
            table.HasCheckConstraint(
                "CK_CustomerRequirements_MaxArea_Positive",
                "[MaxArea] IS NULL OR [MaxArea] > 0");
            table.HasCheckConstraint(
                "CK_CustomerRequirements_Area_Range",
                "[MinArea] IS NULL OR [MaxArea] IS NULL OR [MinArea] <= [MaxArea]");
            table.HasCheckConstraint(
                "CK_CustomerRequirements_PaymentPlanMonths_Positive",
                "[PaymentPlanMonths] IS NULL OR [PaymentPlanMonths] > 0");
        });
    }
}
