using EstateHub.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Billing;

public sealed class PromotionPackageConfiguration : IEntityTypeConfiguration<PromotionPackage>
{
    public void Configure(EntityTypeBuilder<PromotionPackage> builder)
    {
        builder.HasKey(package => package.Id);

        builder.Property(package => package.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(package => package.Placement)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(package => package.Price)
            .HasPrecision(18, 2);

        builder.Property(package => package.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.HasIndex(package => package.Name)
            .HasDatabaseName("UX_PromotionPackages_Name")
            .IsUnique();

        builder.HasOne(package => package.Currency)
            .WithMany(currency => currency.PromotionPackages)
            .HasForeignKey(package => package.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_PromotionPackages_DurationDays_Positive",
                "[DurationDays] > 0");
            table.HasCheckConstraint(
                "CK_PromotionPackages_Price_NonNegative",
                "[Price] >= 0");
        });
    }
}
