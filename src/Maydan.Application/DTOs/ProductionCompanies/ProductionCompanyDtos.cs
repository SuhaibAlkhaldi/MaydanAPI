namespace Maydan.Application.DTOs.ProductionCompanies;

// different convention here (unlike CityLocationDto, which had to match an already-built,
// already-real frontend service), so this follows the codebase's more common prefixed pattern.
//
// Associated users are NOT embedded here — MAYD-81's own note says to reuse the entity-scoped
// GET /api/users?entityType=&entityId= mechanism (MAYD-20) for a company's users, the same real
// endpoint Phase 2b/2e already wired the Users List page and the Supervisors picker to, rather
// than duplicating that data into this DTO or inventing a third way to list them.
public class ProductionCompanyDto
{
    public int Id { get; set; }
    public string EnglishName { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;

    public int CityId { get; set; }
    public string CityEnglishName { get; set; } = string.Empty;
    public string CityArabicName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
    public bool IsSelfRegistered { get; set; }
    public bool IsDeleted { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
}

// MAYD-82: same {IsActive} shape as UserManagementService's own UpdateUserStatusDto — one bool,
// no separate reason/comment field asked for by either ticket.
public class UpdateProductionCompanyStatusDto
{
    public bool IsActive { get; set; }
}
