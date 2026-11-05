using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

/// <summary>
/// EN: EF Core configuration for ProjectAssociation entity, defining keys, relationships, and performance indexes.
/// AR: إعدادات EF Core لجدول ربط المشاريع بالجمعيات، وتحديد المفاتيح والعلاقات وفهارس الأداء.
/// </summary>
public class ProjectAssociationConfiguration : IEntityTypeConfiguration<ProjectAssociation>
{
    public void Configure(EntityTypeBuilder<ProjectAssociation> builder)
    {
        builder.ToTable("ProjectAssociations");

        builder.HasKey(pa => pa.Id);

        // EN: Restrict delete on Project and Association to prevent accidental cascade data loss.
        // AR: منع الحذف المتسلسل للحفاظ على سلامة البيانات المرجعية للمشاريع والجمعيات.
        builder.HasOne(pa => pa.Project)
            .WithMany()
            .HasForeignKey(pa => pa.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pa => pa.Association)
            .WithMany()
            .HasForeignKey(pa => pa.AssociationId)
            .OnDelete(DeleteBehavior.Restrict);

        // EN: One-to-Many relationship with Locations.
        // AR: علاقة واحد لمتعدد مع مواقع الجمعية داخل المشروع.
        builder.HasMany(pa => pa.Locations)
            .WithOne(l => l.ProjectAssociation)
            .HasForeignKey(l => l.ProjectAssociationId)
            .OnDelete(DeleteBehavior.Cascade);

        // EN: Filtered unique index ensuring an association is not linked twice to the same project (when not soft-deleted).
        // AR: فهرس فريد مفلتر يمنع تكرار ربط نفس الجمعية بنفس المشروع طالما السجل غير محذوف.
        builder.HasIndex(pa => new { pa.ProjectId, pa.AssociationId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}