using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

public class LookupCategoryConfiguration : IEntityTypeConfiguration<LookupCategory>
{
    public void Configure(EntityTypeBuilder<LookupCategory> builder)
    {
        builder.ToTable("LookupCategories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).IsRequired().HasMaxLength(100);
        builder.Property(c => c.ArabicName).IsRequired().HasMaxLength(150);
        builder.Property(c => c.EnglishName).IsRequired().HasMaxLength(150);

        builder.HasIndex(c => c.Code).IsUnique();
    }
}
