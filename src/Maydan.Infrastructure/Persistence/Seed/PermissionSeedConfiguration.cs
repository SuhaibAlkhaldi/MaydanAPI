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
                IsDeleted = false
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

                // Production Houses
                Seed(21, "View Production Houses", "عرض بيوت الإنتاج", "ProductionHouses"),
                Seed(22, "Manage Production Houses", "إدارة بيوت الإنتاج", "ProductionHouses"),

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

                // Locations — product decision (2026-09-21): Bayt-AlUrdon-only for now (general
                // reference catalog: countries/cities/city-locations), not entity-scoped.
                Seed(31, "View Locations", "عرض المواقع", "Locations"),
                Seed(32, "Manage Locations", "إدارة المواقع", "Locations"),

                // Attendance — product decision (2026-09-21): View for all 4 real roles (oversight
                // for Bayt-AlUrdon/ASEZA, own-project visibility for ProductionHouse/Association);
                // Manage (recording/approving scans) restricted to Association + ProductionHouse,
                // the two entities actually involved in the double-verification workflow.
                Seed(33, "View Attendance", "عرض الحضور", "Attendance"),
                Seed(34, "Manage Attendance", "إدارة الحضور", "Attendance"),

                // Payments — product decision (2026-09-21): View for all 4 real roles (payroll has
                // no approval workflow, just direct calculation/display); Manage restricted to
                // Bayt-AlUrdon + ProductionHouse (the company paying), not Association or ASEZA.
                Seed(35, "View Payments", "عرض الرواتب", "Payments"),
                Seed(36, "Manage Payments", "إدارة الرواتب", "Payments"),

                // Entity onboarding Stage 2 (2026-09-22): gates picking an existing, admin-less
                // Association and creating its first admin user (Bayt-AlUrdon only — Associations
                // are pre-seeded from GIS data, never created here). Deliberately its own dedicated
                // permission, not a reuse of ManageAssociations/ManageUsers — see
                // RolePermissionSeedConfiguration.cs's own comment on why.
                Seed(37, "Onboard Entities", "استيعاب الجهات", "Onboarding"),

                // System Configuration gate (MAYD-133, 2026-09-24): gates the SMTP configuration
                // admin screen (SystemConfigurationController). Bayt-AlUrdon only — same
                // dedicated-permission shape as Onboard Entities above, not a reuse of ManageUsers.
                Seed(38, "Manage System Configuration", "إدارة إعدادات النظام", "SystemConfiguration"),

                // Closes the real gap where nobody could activate/deactivate an Association's own
                // users: Bayt-AlUrdon/ASEZA hold ManageUsers but UpdateUserStatusAsync's
                // GetScopedUserAsync call is strict same-entity with no cross-entity override (by
                // design — see that method's own comment), and the Association role never held
                // ManageUsers itself. Self-service, not a cross-entity override — same module as
                // ViewAssociationUsers (id 14) rather than "Users", and deliberately its own
                // dedicated permission rather than granting the Association role the broader
                // ManageUsers (which would also unlock UpdateDirectPermissionsAsync/UpdateGroupsAsync
                // for its own users, well beyond this gap's actual scope).
                Seed(39, "Manage Association Users", "إدارة مستخدمي الجمعيات", "Associations"),

                // Phase 1 of the ProductionHouse self-service user-management gap (same root cause,
                // same fix shape as id 39 above): nobody with the ProductionHouse role holds
                // ManageUsers either (confirmed in RolePermissionSeedConfiguration.cs's
                // productionHousePermissions array), so UpdateUserStatusAsync rejected every
                // ProductionHouse account trying to activate/deactivate its own users. Same module as
                // ViewProductionCompanies/ManageProductionCompanies (ids 19/20) rather than "Users",
                // and deliberately its own dedicated permission for the same reason id 39 is — this
                // only ever covers activate/deactivate, not the broader ManageUsers surface.
                Seed(40, "Manage Production Company Users", "إدارة مستخدمي شركة الإنتاج", "ProductionCompanies")
            );
        }
    }
}
