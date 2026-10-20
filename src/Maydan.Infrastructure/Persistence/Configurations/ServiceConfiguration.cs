using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.NameEn)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.NameAr)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Price)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(s => s.CalculationType).IsRequired();

        builder.Property(s => s.RequiresDocuments).HasDefaultValue(false);

        builder.Property(s => s.IsActive).HasDefaultValue(true);

        // Unique indexes on names among non-deleted rows. Use SQL Server filtered unique
        // indexes so soft-deleted rows (IsDeleted = 1) do not block reuse of names.
        // The filter uses the actual column name for IsDeleted as mapped by SharedEntities.
        // EF Core doesn't have a cross-provider fluent API for filtered indexes, but the
        // SQL Server provider supports HasFilter. Use it here to generate the correct
        // migration for SQL Server while keeping model configuration explicit.
        // For SQL Server, specify the filtered unique index to apply only to non-deleted rows.
        // Use the exact column name "IsDeleted" as mapped by SharedEntities.
        builder.HasIndex(s => s.NameEn)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(s => s.NameAr)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
