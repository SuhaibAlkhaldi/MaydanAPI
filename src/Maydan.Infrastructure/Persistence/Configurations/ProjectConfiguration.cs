using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects", table =>
        {
            table.HasCheckConstraint(
                "CK_Projects_EndDate_After_StartDate",
                "[EndDate] > [StartDate]");

            table.HasCheckConstraint(
                "CK_Projects_Producer_LocationManager",
                "[ProducerUserId] <> [LocationManagerUserId]");
        });
        builder.HasOne(p => p.City)
       .WithMany()
       .HasForeignKey(p => p.CityId)
       .IsRequired()
       .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.CityLocation)
               .WithMany()
               .HasForeignKey(p => p.CityLocationId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProjectNameEn)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.ProjectNameAr)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.StartDate)
            .IsRequired();

        builder.Property(p => p.EndDate)
            .IsRequired();

        builder.Property(p => p.WorkPermitImagePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(p => p.ProductionCompany)
            .WithMany(pc => pc.Projects)
            .HasForeignKey(p => p.ProductionCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ProjectType)
            .WithMany(pt => pt.Projects)
            .HasForeignKey(p => p.ProjectTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Producer)
            .WithMany()
            .HasForeignKey(p => p.ProducerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.LocationManager)
            .WithMany()
            .HasForeignKey(p => p.LocationManagerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}