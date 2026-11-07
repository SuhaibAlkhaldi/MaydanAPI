using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

/// <summary>
/// EN: EF Core configuration for ProjectWorker entity, including financial precision for DailyWageAmount.
/// AR: إعدادات EF Core لجدول عمال المشروع، شاملة الدقة العشرية لحقل الأجر اليومي.
/// </summary>
public class ProjectWorkerConfiguration : IEntityTypeConfiguration<ProjectWorker>
{
    public void Configure(EntityTypeBuilder<ProjectWorker> builder)
    {
        builder.ToTable("ProjectWorkers");

        builder.HasKey(pw => pw.Id);

        // EN: Standard financial precision (18,2) matching WorkerProjectLinkConfiguration.
        // AR: تحديد الدقة العشرية المالية (18,2) للأجر اليومي لضمان دقة حسابات الرواتب.
        builder.Property(pw => pw.DailyWageAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(pw => pw.IsWorkerActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasOne(pw => pw.Project)
            .WithMany()
            .HasForeignKey(pw => pw.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pw => pw.Worker)
            .WithMany()
            .HasForeignKey(pw => pw.WorkerId)
            .OnDelete(DeleteBehavior.Restrict);

        // EN: Filtered unique index preventing duplicate assignment of the same worker to the same project.
        // AR: فهرس فريد مفلتر يمنع تكرار إضافة نفس العامل لنفس المشروع طالما السجل غير محذوف.
        builder.HasIndex(pw => new { pw.ProjectId, pw.WorkerId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // EN: Composite index to optimize filtering by ProjectId and IsWorkerActive on the GET project/{projectId} endpoint.
        // AR: فهرس مركب لتحسين سرعة الاستعلام والفلترة حسب المشروع وحالة نشاط العامل.
        builder.HasIndex(pw => new { pw.ProjectId, pw.IsWorkerActive });
    }
}