using EstateHub.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(project => project.Id);

        builder.Property(project => project.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(project => project.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(project => project.Description)
            .HasMaxLength(4000);

        builder.Property(project => project.RowVersion)
            .IsRowVersion();

        builder.HasIndex(project => new { project.DeveloperCompanyId, project.Slug })
            .HasDatabaseName("UX_Projects_DeveloperCompanyId_Slug")
            .IsUnique();

        builder.HasOne(project => project.DeveloperCompany)
            .WithMany(company => company.Projects)
            .HasForeignKey(project => project.DeveloperCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(project => project.Location)
            .WithMany(location => location.Projects)
            .HasForeignKey(project => project.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
