using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

public class AssociationConfiguration : IEntityTypeConfiguration<Association>
{
    public void Configure(EntityTypeBuilder<Association> builder)
    {
        builder.ToTable("Associations");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ArabicName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.EnglishName).IsRequired().HasMaxLength(200);
        builder.HasIndex(a => new { a.ArabicName, a.EnglishName }).IsUnique();

        builder.Property(a => a.LocationOnGoogleMaps).HasMaxLength(500);
        builder.Property(a => a.Latitude).HasPrecision(9, 6).IsRequired();
        builder.Property(a => a.Longitude).HasPrecision(9, 6).IsRequired();
        builder.HasIndex(a => new { a.Latitude, a.Longitude }).IsUnique();


        builder.Property(a => a.ContactPhone).HasMaxLength(30);
        builder.HasIndex(a => a.ContactPhone).IsUnique();
        builder.Property(a => a.ContactEmail).HasMaxLength(200);

        builder.HasOne(a => a.City)
            .WithMany(c => c.Associations)
            .HasForeignKey(a => a.CityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
