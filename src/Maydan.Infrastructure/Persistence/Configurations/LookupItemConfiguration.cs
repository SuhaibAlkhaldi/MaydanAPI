using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

public class LookupItemConfiguration : IEntityTypeConfiguration<LookupItem>
{
    public void Configure(EntityTypeBuilder<LookupItem> builder)
    {
        builder.ToTable("LookupItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Code).IsRequired().HasMaxLength(100);
        builder.Property(i => i.ArabicName).IsRequired().HasMaxLength(150);
        builder.Property(i => i.EnglishName).IsRequired().HasMaxLength(150);

        builder.HasOne(i => i.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.CategoryId, i.Code }).IsUnique();
    }
}
