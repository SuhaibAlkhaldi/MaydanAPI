using Maydan.Application.DTOs.Onboarding;
using Maydan.Application.DTOs.UserManagement;
using Maydan.Application.DTOs.Workers;

namespace Maydan.Application.DTOs.Associations;

public class AssociationDto
{
    public int Id { get; set; }
    public string EnglishName { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public string LocationOnGoogleMaps { get; set; } = string.Empty;

    // Stored as decimal (HasPrecision(9,6) in AssociationConfiguration), formatted to string here
    // since the frontend's own Association.latitude/longitude are typed as string (it round-trips
    // them as plain form-input text, not a parsed number — see association-form.component.ts's
    // setMapSelection, which builds them via lat.toFixed(6)).
    public string Latitude { get; set; } = string.Empty;
    public string Longitude { get; set; } = string.Empty;

    public string ContactPhone { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;

    // Association has no CountryId column — this is City.Country.Id, surfaced for the frontend's
    // own read-side convenience (it never needs to be persisted; see CreateAssociationDto's own
    // comment on why the CountryId a caller SENDS is accepted but discarded).
    public int CountryId { get; set; }
    public string CountryEnglishName { get; set; } = string.Empty;
    public string CountryArabicName { get; set; } = string.Empty;

    public int CityId { get; set; }
    public string CityEnglishName { get; set; } = string.Empty;
    public string CityArabicName { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    // Computed COUNT(Workers WHERE AssociationId = Id), per Association.cs's own comment — never a
    // stored column.
    public int WorkersCount { get; set; }
}

public class CreateAssociationDto
{
    public string EnglishName { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public string? LocationOnGoogleMaps { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public int? CountryId { get; set; }
    public int CityId { get; set; }

    public OnboardAssociationAdminDto? Admin { get; set; }
}

public class UpdateAssociationDto : CreateAssociationDto
{
    public int Id { get; set; }
}

// Association Users/Details gap (2026-09-29): AssociationDto itself is deliberately left unchanged
// (used by 5 different endpoints — GetAll/GetById/SearchByName/GetOrderedByWorkersCount/GetDeleted/
// SearchDeletedByName — adding a Users list there would mean populating it on every list row or
// leaving it silently empty depending on which endpoint served it, an implicit contract). This is a
// separate, additive response type for the one new endpoint that actually needs the combination
// (GET api/associations/{id}/details), composed from AssociationDto's own fields plus the same real
// User rows (EntityType.Association + EntityId == association.Id) AssociationService.DeleteAsync's
// cascade and EntityOnboardingService's admin-check already read the same way. Reuses the existing
// UserManagementDtos.UserSummaryDto instead of a new per-association user DTO — same shape, same
// mapping (UserManagementService.MapUserSummary, now internal for this reuse).
public class AssociationDetailsDto : AssociationDto
{
    public List<UserSummaryDto> Users { get; set; } = new();
    public List<WorkerSummaryDto> Workers { get; set; } = new();
}
