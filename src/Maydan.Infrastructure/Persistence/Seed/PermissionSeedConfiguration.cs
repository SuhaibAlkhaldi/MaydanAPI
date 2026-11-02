using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maydan.Infrastructure.Persistence.Seed
{
    public class PermissionSeedConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            var seedDate = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);

            Permission Seed(int id, string nameEn, string nameAr, string module) => new Permission
            {
                PermissionId = id,
                PermissionNameEn = nameEn,
                PermissionNameAr = nameAr,
                Module = module,
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                IsDeleted = false,
               
            };

            builder.HasData(
                // Users
                Seed(1, "View Users", "عرض المستخدمين", "Users"),
                Seed(2, "Create Users", "إنشاء المستخدمين", "Users"),
                Seed(3, "Manage Users", "إدارة المستخدمين", "Users"),

                // Groups
                Seed(4, "View Groups", "عرض المجموعات", "Groups"),
                Seed(5, "Create Groups", "إنشاء المجموعات", "Groups"),
                Seed(6, "Edit Groups", "تعديل المجموعات", "Groups"),
                Seed(7, "Delete Groups", "حذف المجموعات", "Groups"),
                Seed(8, "Manage Groups", "إدارة المجموعات", "Groups"),

                // Associations
                Seed(9, "View Associations", "عرض الجمعيات", "Associations"),
                Seed(10, "Create Associations", "إنشاء الجمعيات", "Associations"),
                Seed(11, "Edit Associations", "تعديل الجمعيات", "Associations"),
                Seed(12, "Delete Associations", "حذف الجمعيات", "Associations"),
                Seed(13, "Manage Associations", "إدارة الجمعيات", "Associations"),
                Seed(14, "View Association Users", "عرض مستخدمي الجمعيات", "Associations"),

                // Service Requests
                Seed(15, "Request Service", "طلب خدمة", "ServiceRequests"),
                Seed(16, "View Service Requests", "عرض طلبات الخدمة", "ServiceRequests"),
                Seed(17, "Manage Service Requests", "إدارة طلبات الخدمة", "ServiceRequests"),
                Seed(18, "Manage Services", "إدارة الخدمات", "ServiceRequests"),

                // Production Companies
                Seed(19, "View Production Companies", "عرض شركات الإنتاج", "ProductionCompanies"),
                Seed(20, "Manage Production Companies", "إدارة شركات الإنتاج", "ProductionCompanies"),

                // Workers
                Seed(23, "View Workers", "عرض العمال", "Workers"),
                Seed(24, "Manage Workers", "إدارة العمال", "Workers"),

                // Projects
                Seed(25, "View Projects", "عرض المشاريع", "Projects"),
                Seed(26, "Create Projects", "إنشاء المشاريع", "Projects"),
                Seed(27, "Edit Projects", "تعديل المشاريع", "Projects"),
                Seed(28, "Delete Projects", "حذف المشاريع", "Projects"),
                Seed(29, "Review Projects", "مراجعة المشاريع", "Projects"),
                Seed(30, "Manage Projects", "إدارة المشاريع", "Projects"),

                // Locations
                Seed(31, "View Locations", "عرض المواقع", "Locations"),
                Seed(32, "Manage Locations", "إدارة المواقع", "Locations"),

                // Attendance
                Seed(33, "View Attendance", "عرض الحضور", "Attendance"),
                Seed(34, "Manage Attendance", "إدارة الحضور", "Attendance"),

                // Payments
                Seed(35, "View Payments", "عرض الرواتب", "Payments"),
                Seed(36, "Manage Payments", "إدارة الرواتب", "Payments"),

                // Onboarding
                Seed(37, "Onboard Entities", "استيعاب الجهات", "Onboarding"),

                // System Configuration
                Seed(38, "Manage System Configuration", "إدارة إعدادات النظام", "SystemConfiguration"),

                // Association Users
                Seed(39, "Manage Association Users", "إدارة مستخدمي الجمعيات", "Associations"),

                // Production Company Users
                Seed(40, "Manage Production Company Users", "إدارة مستخدمي شركة الإنتاج", "ProductionCompanies"),
                Seed(41, "View Total Amount for Service Request", "عرض إجمالي تكلفة طلب الخدمة", "ProductionCompanies"),
                //view TOT-AM  Cause we need to know the how much cost when prod company request service from association
                Seed(42, "View Audit Trail","تسجيل العمليات اين ومتى تمت", "Aseza,BaytAlUrdon"),
                Seed(44, "Manage Worker Suggestion Rules","إدارة قواعد اقتراح العمال","WorkerSuggestions")
            );
        }
    }
}