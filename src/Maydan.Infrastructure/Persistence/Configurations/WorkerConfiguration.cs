using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

public class WorkerConfiguration : IEntityTypeConfiguration<Worker>
{
    public void Configure(EntityTypeBuilder<Worker> builder)
    {
        builder.ToTable("Workers");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(w => w.MiddleName).HasMaxLength(100);
        builder.Property(w => w.LastName).IsRequired().HasMaxLength(100);

        // Repo-hygiene fix (2026-09-26): 256 chars is sized for future ciphertext, not the raw
        // national ID length — but that encryption doesn't exist yet (see Worker.CivilId's own
        // comment; CivilId is currently plaintext). No unique index either way: uniqueness is
        // enforced via CivilIdHash (blind index) instead, since ciphertext won't be directly
        // comparable once encryption is actually implemented.
        builder.Property(w => w.CivilId).IsRequired().HasMaxLength(256);

        // HMAC-SHA256 hex digest is always 64 chars.
        builder.Property(w => w.CivilIdHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(w => w.CivilIdHash).IsUnique();

        builder.Property(w => w.PhoneNumber).HasMaxLength(30);

        builder.Property(w => w.Nationality).HasMaxLength(100);

        builder.Property(w => w.QrCode).IsRequired().HasMaxLength(200);
        builder.HasIndex(w => w.QrCode).IsUnique();

        builder.Property(w => w.IsActive).HasDefaultValue(true);

        builder.HasOne(w => w.Association)
            .WithMany(a => a.Workers)
            .HasForeignKey(w => w.AssociationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Worker full-profile fields (Phase 1, 2026-09-30): reuse the existing Country/City
        // reference tables (same ones Association already uses) instead of a free-text Address
        // column — optional (nullable FK) since these are manually entered for now.
        builder.HasOne(w => w.Country)
            .WithMany()
            .HasForeignKey(w => w.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.City)
            .WithMany()
            .HasForeignKey(w => w.CityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
