using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

public class AssociationZoneConfiguration : IEntityTypeConfiguration<AssociationZone>
{
    public void Configure(EntityTypeBuilder<AssociationZone> builder)
    {
        builder.ToTable("AssociationZones");

        builder.HasKey(z => z.Id);

        builder.Property(z => z.ArabicName).IsRequired().HasMaxLength(200);
        builder.Property(z => z.EnglishName).IsRequired().HasMaxLength(200);

        // Spatial column — SQL Server geography type, SRID 4326 (WGS-84).
        builder.Property(z => z.Boundary)
            .HasColumnType("geography")
            .IsRequired();

        builder.Property(z => z.BookedFrom).IsRequired(false);
        builder.Property(z => z.BookedUntil).IsRequired(false);

        builder.HasOne(z => z.Association)
            .WithMany(a => a.Zones)
            .HasForeignKey(z => z.AssociationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional link to a service request that triggered the booking.
        builder.HasOne(z => z.ServiceRequest)
            .WithMany()
            .HasForeignKey(z => z.ServiceRequestId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Ignore the computed property — not stored in DB.
        builder.Ignore(z => z.IsBookedNow);
    }
}
