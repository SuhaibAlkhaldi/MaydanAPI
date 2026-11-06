using Maydan.Domain.Entities;
using Maydan.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

public class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("ServiceRequests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.UnitPriceSnapshot).HasPrecision(18, 2);
        builder.Property(r => r.ExpectedTotalAmount).HasPrecision(18, 2);
        builder.Property(r => r.AdditionalRequirements).HasMaxLength(1000);

        builder.Property(r => r.StatusId)
            .IsRequired()
            .HasDefaultValue(LookupSeed.ServiceRequestStatusPendingWorkerSelectionItemId);

        builder.HasOne(r => r.Status)
            .WithMany()
            .HasForeignKey(r => r.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Project)
            .WithMany()
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ProductionCompany)
            .WithMany()
            .HasForeignKey(r => r.ProductionCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Service)
            .WithMany(s => s.ServiceRequests)
            .HasForeignKey(r => r.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Association)
            .WithMany()
            .HasForeignKey(r => r.AssociationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
