using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

/// <summary>
/// EN: EF Core configuration for ProjectAssociationLocation entity.
/// AR: إعدادات EF Core لجدول المواقع الجغرافية التابعة لجمعية المشروع.
/// </summary>
public class ProjectAssociationLocationConfiguration : IEntityTypeConfiguration<ProjectAssociationLocation>
{
    public void Configure(EntityTypeBuilder<ProjectAssociationLocation> builder)
    {
        builder.ToTable("ProjectAssociationLocations");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.Description)
            .HasMaxLength(500);

        builder.Property(l => l.LocationOnGoogleMaps)
            .HasMaxLength(1000);

        builder.Property(l => l.Latitude)
            .HasMaxLength(50);

        builder.Property(l => l.Longitude)
            .HasMaxLength(50);

        // EN: Index on Foreign Key for fast JOINs and lookups when loading locations for a ProjectAssociation.
        // AR: فهرس على المفتاح الأجنبي لتسريع عمليات الجلب والربط للمواقع التابعة للجمعية.
        builder.HasIndex(l => l.ProjectAssociationId);
    }
}