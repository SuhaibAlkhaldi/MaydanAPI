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
    // Grants each of the 4 real seeded roles (RoleSeedConfiguration) a subset of the permissions
    // seeded by PermissionSeedConfiguration, per the approved permission-matrix proposal.
    public class RolePermissionSeedConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        private const int BaytAlUrdon = 1;
        private const int Aseza = 2;
        private const int ProductionHouse = 3;
        private const int Association = 4;

        // Permission ids, matching PermissionSeedConfiguration.
        private const int ViewUsers = 1;
        private const int CreateUsers = 2;
        private const int ManageUsers = 3;
        private const int ViewGroups = 4;
        private const int CreateGroups = 5;
        private const int EditGroups = 6;
        private const int DeleteGroups = 7;
        private const int ManageGroups = 8;
        private const int ViewAssociations = 9;
        private const int CreateAssociations = 10;
        private const int EditAssociations = 11;
        private const int DeleteAssociations = 12;
        private const int ManageAssociations = 13;
        private const int ViewAssociationUsers = 14;
        private const int RequestService = 15;
        private const int ViewServiceRequests = 16;
        private const int ManageServiceRequests = 17;
        private const int ManageServices = 18;
        private const int ViewProductionCompanies = 19;
        private const int ManageProductionCompanies = 20;
        private const int ViewProductionHouses = 21;
        private const int ManageProductionHouses = 22;
        private const int ViewWorkers = 23;
        private const int ManageWorkers = 24;
        private const int ViewProjects = 25;
        private const int CreateProjects = 26;
        private const int EditProjects = 27;
        private const int DeleteProjects = 28;
        private const int ReviewProjects = 29;
        private const int ManageProjects = 30;

        // Locations/Attendance/Payments — added by the RBAC gaps ticket (2026-09-21), matching
        // PermissionSeedConfiguration.cs ids 31-36.
        private const int ViewLocations = 31;
        private const int ManageLocations = 32;
        private const int ViewAttendance = 33;
        private const int ManageAttendance = 34;
        private const int ViewPayments = 35;
        private const int ManagePayments = 36;

        // Entity onboarding Stage 2 (2026-09-22) — matching PermissionSeedConfiguration.cs id 37.
        // Bayt-AlUrdon only: not added to asezaPermissions/productionHousePermissions/
        // associationPermissions below. A dedicated permission rather than reusing
        // ManageAssociations (which ASEZA also holds, for its own broader oversight, but ASEZA
        // must NOT be able to create an Association's first admin) or ManageUsers (which doesn't
        // exist as a real seeded grant for this action's cross-entity shape at all).
        private const int OnboardEntities = 37;

        // System Configuration gate (MAYD-133, 2026-09-24) — matching PermissionSeedConfiguration.cs
        // id 38. Bayt-AlUrdon only, same as OnboardEntities above.
        private const int ManageSystemConfiguration = 38;

        // Closes the real activate/deactivate gap for Association's own users — matching
        // PermissionSeedConfiguration.cs id 39. See that seed's own comment for why this is
        // self-service (granted to Association below) rather than a Bayt-AlUrdon/ASEZA cross-entity
        // override — GetScopedUserAsync's strict same-entity check is what makes that safe.
        private const int ManageAssociationUsers = 39;

        // Phase 1 of the ProductionHouse self-service user-management gap (same shape as
        // ManageAssociationUsers above) — matching PermissionSeedConfiguration.cs id 40. Same
        // self-service reasoning: granted to ProductionHouse below, not a Bayt-AlUrdon/ASEZA
        // cross-entity override — GetScopedUserAsync's strict same-entity check is what makes that
        // safe here too.
        private const int ManageProductionCompanyUsers = 40;

        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            // ManageServices added per the associations.routes.ts permission-review correction:
            // ASEZA has broad association oversight but no association-CRUD permission, so it was
            // unreachable for the Association's own service-requests inbox (which now gates on
            // ManageServices alone, not a Manage Association(s) CRUD proxy) until granted directly.
            // RBAC gaps ticket (2026-09-21) — product decisions confirmed:
            // - Groups: ASEZA/ProductionHouse/Association each get View+Manage Groups (matches the
            //   confirmed architecture that every entity has an Admin who manages its own
            //   employees' permission groups — MAYD-31/32/33's own spec implied this; the seed
            //   simply never granted it).
            // - Locations: Bayt-AlUrdon only — not added to any of the three arrays below.
            // - Attendance: View for all 4 roles; Manage (recording/approving) for
            //   Association + ProductionHouse only, the two entities in the double-verification
            //   workflow — ASEZA/Bayt-AlUrdon get View only (via ViewAttendance below /
            //   allPermissionIds respectively).
            // - Payments: View for all 4 roles; Manage for Bayt-AlUrdon + ProductionHouse only
            //   (the company paying) — Association/ASEZA get View only.
            // MAYD-1 real fix (2026-09-24, product-owner-confirmed): CreateUsers added — ASEZA can
            // create users within its OWN entity (confirmed real requirement, separate from its
            // cross-entity VIEW-only oversight over Associations/ProductionHouse, which stays
            // exactly as it was — no create/edit grant added for those). Before this, ASEZA held
            // ZERO Create-Users-equivalent permission (confirmed live during the MAYD-36 sweep), so
            // /users/new 403'd via the frontend's own roleGuard (['Create Users', 'Manage Users'])
            // even though nothing else about ASEZA's own-entity user creation was ever broken.
            //
            // MAYD-37 real fix (2026-09-24, product-owner-confirmed): ViewAssociations,
            // ViewProductionCompanies, ViewProductionHouses, ViewWorkers, ViewProjects,
            // ViewAttendance, ViewPayments REMOVED from this role-level array — the ticket's own
            // requirement ("ASEZA Admin has all view permissions by default; a regular ASEZA user
            // gets a custom subset assigned by the Admin") is structurally impossible while these
            // stay here: AuthService.MapAuthUser computes effective permissions as a pure
            // RolePermissions ∪ UserPermissions ∪ GroupPermissions union with no per-user revoke, so
            // anything granted at the ROLE level is automatically held by EVERY ASEZA user, admin
            // and regular alike — no "custom subset" is possible for a role-level grant. These 7 are
            // instead granted directly to the seeded ASEZA admin (UserId 1000) as individual
            // UserPermissions (see UserSeedConfiguration.cs's own migration for this ticket) — a
            // fresh regular ASEZA user now genuinely starts with none of them, and the admin assigns
            // a real subset via the existing, already-verified User Details "Edit Permissions" flow
            // (MAYD-34) — UpdateDirectPermissionsAsync/GetAvailablePermissionsAsync were extended
            // (see GetCallerDelegatablePermissionIds's own comment) so that flow can actually offer
            // and accept a permission the ASEZA ROLE doesn't hold but the assigning ASEZA admin
            // personally does.
            //
            // ManageAssociations REMOVED ENTIRELY, not re-granted anywhere (not even to the admin):
            // the frontend (associations-list/association-details components, associations.routes.ts)
            // treats 'Manage Associations' as a full Create+Edit+Delete+View substitute — ASEZA
            // holding it meant ASEZA could fully create/edit/delete Associations, directly
            // contradicting this ticket's explicit "view-only, no create, edit or delete actions on
            // that data" requirement (Associations is one of the ticket's own named example modules).
            // ManageProductionHouses is deliberately left untouched despite its name: confirmed via
            // ProductionHouseDetailsComponent's own comment that it is NOT actually used to gate any
            // write action anywhere in the frontend today ("that permission covers ASEZA's oversight
            // visibility, not this specific action") — inert, so removing it would be a no-op change
            // with only risk and no real behavior fix, unlike ManageAssociations.
            var asezaPermissions = new[]
            {
                ViewUsers, CreateUsers, ViewAssociationUsers,
                ManageProductionHouses,
                ManageServices,
                ViewGroups, ManageGroups
            };

            // MAYD-20 (2026-09-24): ViewUsers added to both arrays below — previously granted to
            // Bayt-AlUrdon/ASEZA only. The real Business Rule (MAYD-1) explicitly requires
            // "{Entity} Admin (Association, Production House) can view users only within their own
            // entity", which is unreachable without this grant (the endpoint 403s otherwise, not
            // "sees only its own entity" — there's a real difference). Flagged rather than silent:
            // the final Roles/Permissions design is explicitly MAYD-19's call (To Do, a different
            // assignee per the roadmap doc), not settled here — this is the minimal, additive grant
            // this one ticket's own stated requirement needs, not a broader permission redesign.
            var productionHousePermissions = new[]
            {
                ViewUsers,
                ViewProjects, CreateProjects, EditProjects,
                RequestService, ViewServiceRequests,
                ViewWorkers,
                ViewGroups, ManageGroups,
                ViewAttendance, ManageAttendance,
                ViewPayments, ManagePayments,
                ManageProductionCompanyUsers
            };

            var associationPermissions = new[]
            {
                ViewUsers,
                ViewAssociations, EditAssociations, ManageWorkers, ManageServices, ViewAssociationUsers,
                ManageAssociationUsers,
                ViewGroups, ManageGroups,
                ViewAttendance, ManageAttendance,
                ViewPayments
            };

            var allPermissionIds = Enumerable.Range(1, 40);

            var grants = ForRole(BaytAlUrdon, allPermissionIds)
                .Concat(ForRole(Aseza, asezaPermissions))
                .Concat(ForRole(ProductionHouse, productionHousePermissions))
                .Concat(ForRole(Association, associationPermissions));

            builder.HasData(grants);
        }

        private static IEnumerable<RolePermission> ForRole(int roleId, IEnumerable<int> permissionIds)
        {
            return permissionIds.Select(permissionId => new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                IsActive = true
            });
        }
    }
}
