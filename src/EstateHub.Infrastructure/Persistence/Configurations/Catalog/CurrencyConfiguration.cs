using EstateHub.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Catalog;

public sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.HasKey(currency => currency.Code);

        builder.Property(currency => currency.Code)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(currency => currency.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(currency => currency.Symbol)
            .HasMaxLength(16)
            .IsRequired();
    }
}
