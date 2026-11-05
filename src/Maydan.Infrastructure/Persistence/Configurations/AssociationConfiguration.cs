using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

public class AssociationConfiguration : IEntityTypeConfiguration<Association>
{
    public void Configure(EntityTypeBuilder<Association> builder)
    {
        builder.ToTable("Associations");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ArabicName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.EnglishName).IsRequired().HasMaxLength(200);

        builder.Property(a => a.LocationOnGoogleMaps).HasMaxLength(500);
        builder.Property(a => a.Latitude).HasPrecision(9, 6);
        builder.Property(a => a.Longitude).HasPrecision(9, 6);

        // Geofence: stored as SQL Server geography column (SRID 4326).
        // Nullable — legacy associations created before geo support have no boundary.
        builder.Property(a => a.Geofence)
            .HasColumnType("geography")
            .IsRequired(false);

        builder.Property(a => a.ContactPhone).HasMaxLength(30);
        builder.Property(a => a.ContactEmail).HasMaxLength(200);

        // CityId is now optional (nullable FK).
        builder.HasOne(a => a.City)
            .WithMany(c => c.Associations)
            .HasForeignKey(a => a.CityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
