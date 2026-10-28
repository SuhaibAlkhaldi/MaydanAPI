using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

public class WorkerOvertimeConfiguration : IEntityTypeConfiguration<WorkerOvertime>
{
    public void Configure(EntityTypeBuilder<WorkerOvertime> builder)
    {
        builder.ToTable("WorkerOvertimes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.OvertimeForDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.Hours).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.HoursPerUnit).IsRequired();
        builder.Property(x => x.UnitWageSnapshot).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.ApprovalStatus).IsRequired();
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.CreatedDate).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.WorkerProjectLink)
            .WithMany()
            .HasForeignKey(x => x.WorkerProjectLinkId)
            .OnDelete(DeleteBehavior.Restrict);

        // One overtime record per worker per day.
        builder.HasIndex(x => new { x.WorkerProjectLinkId, x.OvertimeForDate }).IsUnique();
        builder.HasIndex(x => x.ApprovalStatus);
    }
}
