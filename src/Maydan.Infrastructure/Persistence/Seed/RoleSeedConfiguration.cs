using Maydan.Domain.Entities;
using Maydan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Maydan.Infrastructure.Persistence.Seed
{
    public class RoleSeedConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            var seedDate = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);

            builder.HasData(
                // Main Entity Roles
                new Role
                {
                    RoleId = 1,
                    RoleNameEn = "Bayt-AlUrdon",
                    RoleNameAr = "بيت الأردن",
                    TargetEntityType = null, 
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate,
                    IsDeleted = false,
                    IsActive=true
                },
                new Role
                {
                    RoleId = 2,
                    RoleNameEn = "ASEZA",
                    RoleNameAr = "سلطة منطقة العقبة الاقتصادية الخاصة",
                    TargetEntityType = null, 
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate,
                    IsDeleted = false,
                    IsActive=true
                },

                new Role
                {
                    RoleId = 3,
                    RoleNameEn = "ProductionHouse",
                    RoleNameAr = "شركة الإنتاج",
                    TargetEntityType = EntityType.ProductionCompany,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate,
                    IsDeleted = false,
                    IsActive = true

                },
                new Role
                {
                    RoleId = 4,
                    RoleNameEn = "Association",
                    RoleNameAr = "الجمعية",
                    TargetEntityType = EntityType.Association,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate,
                    IsDeleted = false,
                    IsActive = true

                },

                // Operational Sub Roless for ProductionCompany
                new Role
                {
                    RoleId = 5,
                    RoleNameEn = "Producer",
                    RoleNameAr = "منتج",
                    TargetEntityType = EntityType.ProductionCompany,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate,
                    IsDeleted = false,
                    IsActive = true

                },
                new Role
                {
                    RoleId = 6,
                    RoleNameEn = "LocationManager",
                    RoleNameAr = "مدير الموقع",
                    TargetEntityType = EntityType.ProductionCompany,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate,
                    IsDeleted = false,
                    IsActive = true

                },

                // Operational Sub  Roles for Association
                new Role
                {
                    RoleId = 7,
                    RoleNameEn = "WorkerManager",
                    RoleNameAr = "مدير العمال",
                    TargetEntityType = EntityType.Association,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate,
                    IsDeleted = false,
                    IsActive = true

                },
                new Role
                {
                    RoleId = 8,
                    RoleNameEn = "Worker",
                    RoleNameAr = "عامل",
                    TargetEntityType = EntityType.Association,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate,
                    IsDeleted = false,
                    IsActive = true

                },
                new Role
                {
                    RoleId = 9,
                    RoleNameEn = "AttendanceOfficer",
                    RoleNameAr = "مسؤول الحضور",
                    TargetEntityType = EntityType.Association,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate,
                    IsDeleted = false,
                    IsActive = true

                },

                // Additional System Admin Role
                new Role
                {
                    RoleId = 10,
                    RoleNameEn = "Admin",
                    RoleNameAr = "مشرف نظام",
                    TargetEntityType = null,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate,
                    IsDeleted = false,
                    IsActive = true

                }
            );
        }
    }
}