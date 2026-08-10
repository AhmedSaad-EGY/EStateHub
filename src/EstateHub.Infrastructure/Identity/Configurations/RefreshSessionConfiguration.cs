using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Identity.Configurations;

public sealed class RefreshSessionConfiguration : IEntityTypeConfiguration<RefreshSession>
{
    public void Configure(EntityTypeBuilder<RefreshSession> builder)
    {
        builder.HasKey(session => session.Id);

        builder.Property(session => session.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(session => session.TokenHash)
            .IsUnique();

        builder.HasOne(session => session.ApplicationUser)
            .WithMany()
            .HasForeignKey(session => session.ApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
