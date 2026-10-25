using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

public class WorkerProjectLinkConfiguration : IEntityTypeConfiguration<WorkerProjectLink>
{
    public void Configure(EntityTypeBuilder<WorkerProjectLink> builder)
    {
        builder.ToTable("WorkerProjectLinks");

        //  PK: surrogate int (auto-increment) 
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Audit columns
        builder.Property(x => x.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.CreatedBy).IsRequired(false);   // int? user ID
        builder.Property(x => x.UpdatedBy).IsRequired(false);   // int? user ID
        builder.Property(x => x.UpdatedDate).IsRequired(false);

        // Business columns
        builder.Property(x => x.DailyWageAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.IsWorkerActive)
            .HasDefaultValue(true)
            .IsRequired();

        //  Relationships (Restrict deletes) 
        builder.HasOne(x => x.Project)
            .WithMany(p => p.WorkerProjectLinks)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Worker)
            .WithMany(w => w.WorkerProjectLinks)
            .HasForeignKey(x => x.WorkerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ServiceRequest)
            .WithMany()
            .HasForeignKey(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique business constraint: one active link per (project, worker) 
        builder.HasIndex(x => new { x.ProjectId, x.WorkerId })
            .IsUnique()
            .HasFilter("[IsWorkerActive] = 1");
    }
}
