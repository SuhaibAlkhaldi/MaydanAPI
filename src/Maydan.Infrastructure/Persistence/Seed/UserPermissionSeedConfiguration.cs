using Maydan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maydan.Infrastructure.Persistence.Seed
{
    // MAYD-37 (2026-09-24, product-owner-confirmed): the first real seeded UserPermission rows in
    // this project — every prior individual grant was always created dynamically at runtime
    // (CreateUserAsync/UpdateDirectPermissionsAsync), never via HasData. Needed here specifically
    // because these page-view permissions were deliberately REMOVED from ASEZA's own
    // RolePermissions (see RolePermissionSeedConfiguration.cs's own comment on why a role-level
    // grant can never support "regular ASEZA user gets a custom subset") — the seeded ASEZA admin
    // (UserId 1000, UserSeedConfiguration.cs) needs them granted directly instead, matching the
    // ticket's own "ASEZA Admin has all view permissions by default" requirement.
    //
    // Permission ids match PermissionSeedConfiguration.cs / RolePermissionSeedConfiguration.cs's own
    // consts: ViewAssociations=9, ViewProductionCompanies=19, ViewWorkers=23,
    // ViewProjects=25, ViewAttendance=33, ViewPayments=35. (ViewProductionHouses ID 21 was removed).
    public class UserPermissionSeedConfiguration : IEntityTypeConfiguration<UserPermission>
    {
        private const int AsezaAdminUserId = 1000;

        private const int ViewAssociations = 9;
        private const int ViewProductionCompanies = 19;
        private const int ViewWorkers = 23;
        private const int ViewProjects = 25;
        private const int ViewAttendance = 33;
        private const int ViewPayments = 35;

        public void Configure(EntityTypeBuilder<UserPermission> builder)
        {
            var pageViewPermissionIds = new[]
            {
                ViewAssociations, ViewProductionCompanies,
                ViewWorkers, ViewProjects, ViewAttendance, ViewPayments
            };

            builder.HasData(pageViewPermissionIds.Select(permissionId => new UserPermission
            {
                UserId = AsezaAdminUserId,
                PermissionId = permissionId,
                IsActive = true
            }));
        }
    }
}