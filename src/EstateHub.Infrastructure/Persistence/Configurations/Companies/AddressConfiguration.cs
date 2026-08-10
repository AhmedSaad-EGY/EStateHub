using EstateHub.Domain.Entities.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Companies;

public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.HasKey(address => address.Id);

        builder.Property(address => address.AddressLine1)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(address => address.AddressLine2)
            .HasMaxLength(300);

        builder.Property(address => address.PostalCode)
            .HasMaxLength(20);

        builder.Property(address => address.Latitude)
            .HasPrecision(9, 6);

        builder.Property(address => address.Longitude)
            .HasPrecision(9, 6);

        builder.HasOne(address => address.Location)
            .WithMany(location => location.Addresses)
            .HasForeignKey(address => address.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_Addresses_Latitude_Range",
                "[Latitude] IS NULL OR ([Latitude] >= -90 AND [Latitude] <= 90)");
            table.HasCheckConstraint(
                "CK_Addresses_Longitude_Range",
                "[Longitude] IS NULL OR ([Longitude] >= -180 AND [Longitude] <= 180)");
        });
    }
}
