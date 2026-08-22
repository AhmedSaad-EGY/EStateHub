using EstateHub.Application.PlatformAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Identity.Configurations;

public sealed class PlatformRoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public static readonly Guid PlatformAdminRoleId =
        new("30000000-0000-4000-8000-000000000001");

    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
    {
        builder.HasData(new IdentityRole<Guid>
        {
            Id = PlatformAdminRoleId,
            Name = PlatformRoleNames.PlatformAdmin,
            NormalizedName = PlatformRoleNames.NormalizedPlatformAdmin,
            ConcurrencyStamp = "platform-admin-role-v1"
        });
    }
}
