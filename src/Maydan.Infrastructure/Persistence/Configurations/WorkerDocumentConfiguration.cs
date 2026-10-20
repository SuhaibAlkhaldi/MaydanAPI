using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

public class WorkerDocumentConfiguration : IEntityTypeConfiguration<WorkerDocument>
{
    public void Configure(EntityTypeBuilder<WorkerDocument> builder)
    {
        builder.ToTable("WorkerDocuments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.FilePath).IsRequired().HasMaxLength(500);

        builder.HasOne(d => d.Worker)
            .WithMany(w => w.Documents)
            .HasForeignKey(d => d.WorkerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
