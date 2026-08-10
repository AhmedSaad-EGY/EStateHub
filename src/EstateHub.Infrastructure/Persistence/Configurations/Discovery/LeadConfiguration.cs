using EstateHub.Domain.Entities.Discovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Discovery;

public sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.HasKey(lead => lead.Id);

        builder.Property(lead => lead.ContactName)
            .HasMaxLength(200);

        builder.Property(lead => lead.ContactPhone)
            .HasMaxLength(32);

        builder.Property(lead => lead.ContactEmail)
            .HasMaxLength(256);

        builder.Property(lead => lead.Source)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(lead => lead.RowVersion)
            .IsRowVersion();

        builder.HasIndex(lead => lead.SourceBookingId)
            .HasDatabaseName("UX_Leads_SourceBookingId")
            .IsUnique()
            .HasFilter("[SourceBookingId] IS NOT NULL");

        builder.HasOne(lead => lead.Company)
            .WithMany(company => company.Leads)
            .HasForeignKey(lead => lead.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(lead => lead.CustomerProfile)
            .WithMany(profile => profile.Leads)
            .HasForeignKey(lead => lead.CustomerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(lead => lead.SourceBooking)
            .WithOne(booking => booking.SourceLead)
            .HasForeignKey<Lead>(lead => lead.SourceBookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(lead => lead.OwnerEmployee)
            .WithMany(employee => employee.OwnedLeads)
            .HasForeignKey(lead => new { lead.OwnerEmployeeId, lead.CompanyId })
            .HasPrincipalKey(employee => new { employee.Id, employee.CompanyId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_Leads_UsableContact",
                "[CustomerProfileId] IS NOT NULL OR [ContactPhone] IS NOT NULL OR [ContactEmail] IS NOT NULL");
        });
    }
}
