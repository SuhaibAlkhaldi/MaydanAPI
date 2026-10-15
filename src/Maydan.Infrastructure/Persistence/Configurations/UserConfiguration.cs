using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.UserId);

        builder.Property(u => u.FirstNameEn)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(u => u.FirstNameAr)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastNameEn)
                    .IsRequired()
                    .HasMaxLength(100);

        builder.Property(u => u.LastNameAr)
            .IsRequired()
            .HasMaxLength(100);


        builder.Property(u => u.PhoneNumber)
           .IsRequired()
           .HasMaxLength(20);
        builder.HasIndex(u => u.PhoneNumber)
           .IsUnique();

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.MustResetPassword)
           .IsRequired();

        builder.Property(u => u.EntityType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}