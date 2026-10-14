using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

public class AssociationConfigurationV2 : IEntityTypeConfiguration<AssociationV2>
{
    public void Configure(EntityTypeBuilder<AssociationV2> builder)
    {
        builder.ToTable("AssociationsV2");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ArabicName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.EnglishName).IsRequired().HasMaxLength(200);

        // فصل المؤشرات ليكون كل اسم فريداً بشكل مستقل
        builder.HasIndex(a => a.ArabicName).IsUnique();
        builder.HasIndex(a => a.EnglishName).IsUnique();

        builder.Property(a => a.LocationOnGoogleMaps).HasMaxLength(500);
        builder.Property(a => a.Latitude).HasPrecision(9, 6).IsRequired();
        builder.Property(a => a.Longitude).HasPrecision(9, 6).IsRequired();
        builder.HasIndex(a => new { a.Latitude, a.Longitude }).IsUnique();

        builder.HasIndex(a => a.ContactPhone)
        .IsUnique()
         .HasFilter("[ContactPhone] IS NOT NULL AND [ContactPhone] != ''");

        builder.HasIndex(a => a.ContactEmail)
        .IsUnique()
        .HasFilter("[ContactEmail] IS NOT NULL AND [ContactEmail] != ''");

        builder.Property(a => a.ContactPhone).IsRequired().HasMaxLength(30);
        builder.Property(a => a.ContactEmail).IsRequired().HasMaxLength(200);

        builder.HasOne(a => a.City)
            .WithMany(c => c.AssociationsV2)
            .HasForeignKey(a => a.CityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}