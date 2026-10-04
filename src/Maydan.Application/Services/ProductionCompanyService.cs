using Maydan.Application.DTOs.ProductionCompanies;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

// Production House Management (MAYD-80 List, MAYD-81 Details, MAYD-82 Activate/Deactivate).
// ProductionCompany.cs, IProductionCompanyRepository/ProductionCompanyRepository (bare
// GetByIdAsync/GetAllAsync/RegistrationNumberExistsAsync/AddAsync/Remove), and the public
// self-registration flow (ProductionCompanyOnboardingService, MAYD-79) already existed — there was
// no ProductionCompanyController at all (AuthController.RegisterProductionCompany's own comment
// says so explicitly), so every real page under workforcment's features/production-companies/ 404'd
// or rendered an empty stub. This class + ProductionCompanyController are what this phase adds.
//
// Deliberately NOT built here (see this phase's own completion report): company-form.component.ts
// (no MAYD-79/80/81/82 ticket describes an internal admin create/edit flow — self-registration is
// the only real creation path), deleted-companies.component.ts / any soft-delete-and-restore flow
// (no ticket describes deleting a Production Company at all), and company-supervisors.component.ts
// (confirmed empty, unrelated future-feature stub, untouched).
public class ProductionCompanyService : IProductionCompanyService
{
    // Real permission catalog ids (PermissionSeedConfiguration.cs): ViewProductionCompanies(19),
    // ManageProductionCompanies(20). Confirmed via RolePermissionSeedConfiguration.cs +
    // 20260924175327_AsezaPageViewPermissions.cs's own migration that ManageProductionCompanies is
    // held ONLY by Bayt-AlUrdon's blanket allPermissionIds grant (Enumerable.Range(1, 38)) — ASEZA
    // never holds it, neither at the role level (removed from asezaPermissions entirely) nor as one
    // of the seeded ASEZA admin's (UserId 1000) individual UserPermissions (that grant covers
    // ViewProductionCompanies only, permission id 19, not 20). That's exactly what MAYD-82 asks for
    // ("Super Admin only, not ASEZA Admin") — confirmed correct here, not a gap needing a fix.
    private const int ViewProductionCompaniesPermissionId = 19;
    private const int ManageProductionCompaniesPermissionId = 20;

    private readonly IUnitOfWork _unitOfWork;

    public ProductionCompanyService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ProductionCompanyDto>> GetAllAsync(int currentUserId, string? search, bool isDeleted, CancellationToken cancellationToken = default)
    {
        await GetAuthorizedUserAsync(currentUserId, ViewProductionCompaniesPermissionId, cancellationToken);

        var companies = await _unitOfWork.ProductionCompanies.QueryAsync(isDeleted, search, cancellationToken);
        return companies.Select(MapToDto).ToList();
    }

    // Phase 3 of the ProductionHouse self-service user-management gap: a self-view carve-out,
    // deliberately NOT added to the shared GetAuthorizedUserAsync below. ViewProductionCompanies/
    // ManageProductionCompanies are oversight-only by design (MAYD-82: "Super Admin only," confirmed
    // in this class's own const comment) — granting ProductionHouse either would also widen
    // GetAllAsync into a full company list, which is explicitly the wrong fix. This is a pure
    // entity-identity comparison instead, the same principle
    // UserManagementService.GetScopedUserAsync already uses for writes ("a caller can always act on
    // its own entity's own record"), applied here to a read: a ProductionHouse caller can view ITS
    // OWN company's record even without the oversight permission, nothing else. GetAllAsync and
    // UpdateStatusAsync both keep calling the untouched GetAuthorizedUserAsync exactly as before —
    // no self-view exception for the full list, and deactivating a company stays Super-Admin-only,
    // never self-service. Duplicating GetAuthorizedUserAsync's own body here (rather than adding a
    // parameter to it) matches this class's own established convention of duplicating
    // permission-checking logic per use site rather than sharing it — see GetEffectivePermissionIds'
    // own comment.
    public async Task<ProductionCompanyDto> GetByIdAsync(int currentUserId, int productionCompanyId, CancellationToken cancellationToken = default)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user was not found.");

        if (!currentUser.IsActive)
        {
            throw new UnauthorizedAccessException("Current user is inactive.");
        }

        var isOwnCompany = currentUser.EntityType == EntityType.ProductionCompany && currentUser.EntityId == productionCompanyId;
        if (!isOwnCompany && !GetEffectivePermissionIds(currentUser).Overlaps(new[] { ViewProductionCompaniesPermissionId, ManageProductionCompaniesPermissionId }))
        {
            throw new UnauthorizedAccessException("Caller does not hold the required Production Companies permission.");
        }

        var company = await _unitOfWork.ProductionCompanies.GetByIdAsync(productionCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException("Production company was not found.");

        return MapToDto(company);
    }

    // MAYD-82 (product-owner-confirmed, 2026-09-26): deactivating a Production Company cascades —
    // every real User row under it (EntityType.ProductionCompany, EntityId == company.Id) is set
    // IsActive = false too, in the SAME SaveChangesAsync call, reusing the exact IsActive mechanism
    // UserManagementService.UpdateUserStatusAsync already established (never IsDeleted, no new
    // mechanism). Reactivating the company deliberately does NOT cascade back — this asymmetry is
    // structural, not an oversight: IsActive is a plain bool on SharedEntities with no accompanying
    // timestamp (unlike IsDeleted/DeletedAt, which is what let Phase 2c's Association cascade-delete
    // build a real symmetric restore by comparing DeletedAt instants for exact equality). With no
    // way to tell "deactivated by this company-level cascade" apart from "deactivated independently,
    // for an unrelated reason, before or after," blindly reactivating every currently-inactive user
    // under the company risks silently reviving someone who was deactivated on purpose. So
    // reactivating only flips the company's own flag; each user is reactivated individually
    // afterward via the existing, already-built per-user Activate/Deactivate flow
    // (UserManagementService.UpdateUserStatusAsync) — deliberately left to that flow, not solved
    // here, and not solved by adding a new DeactivatedAt column either (a bigger schema decision
    // than this ticket asked for).
    public async Task<ProductionCompanyDto> UpdateStatusAsync(int currentUserId, int productionCompanyId, UpdateProductionCompanyStatusDto dto, CancellationToken cancellationToken = default)
    {
        await GetAuthorizedUserAsync(currentUserId, ManageProductionCompaniesPermissionId, cancellationToken);

        var company = await _unitOfWork.ProductionCompanies.GetByIdAsync(productionCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException("Production company was not found.");

        company.IsActive = dto.IsActive;

        if (!dto.IsActive)
        {
            var users = await _unitOfWork.Users.GetByEntityAsync(EntityType.ProductionCompany, company.Id, search: null, cancellationToken);
            foreach (var user in users.Where(u => u.IsActive))
            {
                user.IsActive = false;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(company);
    }

    // Same shape as AssociationService.GetAuthorizedUserAsync: "the specific action's own
    // permission OR ManageProductionCompaniesPermissionId". For UpdateStatusAsync, the required id
    // IS ManageProductionCompaniesPermissionId, so the set naturally collapses to {Manage} only —
    // View alone does not satisfy a Manage-required check, no special-casing needed.
    private async Task<User> GetAuthorizedUserAsync(int currentUserId, int requiredPermissionId, CancellationToken cancellationToken)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user was not found.");

        if (!currentUser.IsActive)
        {
            throw new UnauthorizedAccessException("Current user is inactive.");
        }

        if (!GetEffectivePermissionIds(currentUser).Overlaps(new[] { requiredPermissionId, ManageProductionCompaniesPermissionId }))
        {
            throw new UnauthorizedAccessException("Caller does not hold the required Production Companies permission.");
        }

        return currentUser;
    }

    // Same shape as AssociationService/UserManagementService's own GetEffectivePermissionIds —
    // deliberately duplicated per-service rather than shared, matching this codebase's established
    // convention (see UserManagementService.GetEffectivePermissionIds's own comment on why).
    private static HashSet<int> GetEffectivePermissionIds(User user)
    {
        var rolePermissionIds = user.Role.RolePermissions
            .Where(rp => rp.IsActive && rp.Permission.IsActive)
            .Select(rp => rp.PermissionId);

        var directPermissionIds = user.UserPermissions
            .Where(up => up.IsActive && up.Permission.IsActive)
            .Select(up => up.PermissionId);

        var groupPermissionIds = user.UserGroups
            .Where(ug => ug.Group.IsActive)
            .SelectMany(ug => ug.Group.GroupPermissions)
            .Where(gp => gp.IsActive && gp.Permission.IsActive)
            .Select(gp => gp.PermissionId);

        return rolePermissionIds.Concat(directPermissionIds).Concat(groupPermissionIds).ToHashSet();
    }

    private static ProductionCompanyDto MapToDto(ProductionCompany company) => new()
    {
        Id = company.Id,
        EnglishName = company.EnglishName,
        ArabicName = company.ArabicName,
        RegistrationNumber = company.RegistrationNumber,
        CityId = company.CityId,
        CityEnglishName = company.City.EnglishName,
        CityArabicName = company.City.ArabicName,
        CountryId = company.City.Country.Id,
        CountryEnglishName = company.City.Country.EnglishName,
        CountryArabicName = company.City.Country.ArabicName,
        IsActive = company.IsActive,
        IsSelfRegistered = company.IsSelfRegistered,
        IsDeleted = company.IsDeleted
    };
}
