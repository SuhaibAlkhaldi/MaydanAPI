using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

public class WorkerAttendanceConfiguration : IEntityTypeConfiguration<WorkerAttendance>
{
    public void Configure(EntityTypeBuilder<WorkerAttendance> builder)
    {
        builder.ToTable("WorkerAttendances");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.AttendanceForDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.CheckInNumber).IsRequired();
        builder.Property(x => x.EntryType).IsRequired();
        builder.Property(x => x.ApprovalStatus).IsRequired();
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.CreatedDate).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.WorkerProjectLink)
            .WithMany()
            .HasForeignKey(x => x.WorkerProjectLinkId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.City)
            .WithMany()
            .HasForeignKey(x => x.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        // A worker cannot have two rows for the same check-in slot of the same day.
        builder.HasIndex(x => new { x.WorkerProjectLinkId, x.AttendanceForDate, x.CheckInNumber }).IsUnique();
        builder.HasIndex(x => x.AttendanceForDate);
    }
}

public class AttendanceTimeWindowConfiguration : IEntityTypeConfiguration<AttendanceTimeWindow>
{
    public void Configure(EntityTypeBuilder<AttendanceTimeWindow> builder)
    {
        builder.ToTable("AttendanceTimeWindows");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.AttendanceDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.OpensAtUtc).IsRequired();
        builder.Property(x => x.ClosesAtUtc).IsRequired();
        builder.Property(x => x.CreatedDate).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.ServiceRequest)
            .WithMany()
            .HasForeignKey(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ServiceRequestId, x.AttendanceDate }).IsUnique();
    }
}
