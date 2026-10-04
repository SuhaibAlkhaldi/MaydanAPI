using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

public class WorkerServiceLinkConfiguration : IEntityTypeConfiguration<WorkerServiceLink>
{
    public void Configure(EntityTypeBuilder<WorkerServiceLink> builder)
    {
        builder.ToTable("WorkerServiceLinks");

        builder.HasKey(wsl => wsl.Id);

        builder.HasIndex(wsl => new { wsl.WorkerId, wsl.ServiceId }).IsUnique();

        builder.HasOne(wsl => wsl.Worker)
            .WithMany(w => w.WorkerServices)
            .HasForeignKey(wsl => wsl.WorkerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(wsl => wsl.Service)
            .WithMany()
            .HasForeignKey(wsl => wsl.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
