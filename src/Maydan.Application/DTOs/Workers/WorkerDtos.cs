using Maydan.Domain.Enums;

namespace Maydan.Application.DTOs.Workers;

public record WorkerDto(
    int Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    string CivilId,
    DateTime? DateOfBirth,
    Gender? Gender,
    MaritalStatus? MaritalStatus,
    string? Nationality,
    int? CountryId,
    string? CountryNameEn,
    int? CityId,
    string? CityNameEn,
    string? PhoneNumber,
    int? YearsOfExperience,
    int AssociationId,
    string AssociationNameEn,
    string QrCode,
    List<int> ServiceIds,
    List<string> ServiceNames,
    List<string> DocumentPaths,
    bool IsActive);

public record WorkerSummaryDto(
    int Id,
    string FirstName,
    string LastName,
    string CivilId,
    List<string> ServiceNames,
    int? YearsOfExperience,
    string? PhoneNumber);

public class CreateWorkerDto
{
    // Required only when the caller is Bayt-AlUrdon (must pick a target association explicitly).
    // An Association-role caller may omit it (defaults to their own entity) but is rejected with
    // 400 if they send a different association's id — see WorkerService.ResolveAssociationIdAsync.
    public int? AssociationId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string CivilId { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public MaritalStatus MaritalStatus { get; set; }
    public string Nationality { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;
    public List<int> ServiceIds { get; set; } = new();
    public int YearsOfExperience { get; set; }

    // Set by the controller after saving each uploaded file (see IFileStorageService). Required
    // (1-4 paths) only when one of ServiceIds is flagged Service.RequiresDocuments — enforced in
    // WorkerService.CreateAsync, not here.
    public List<string> DocumentPaths { get; set; } = new();
}

// Deliberately has no AssociationId and no CivilId — both are permanent once the worker is
// created (see WorkerService's own comment on why), so the field simply doesn't exist on this
// DTO rather than existing-but-ignored.
//
// Partial update: every field below is optional — a field left null (not sent) keeps its
// current stored value, same rule as UpdateServiceDto. One consequence: this endpoint can no
// longer explicitly clear an existing MiddleName (null now means "don't touch" instead of
// "clear it") — a deliberate trade-off for consistency with every other field here.
public class UpdateWorkerDto
{
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public MaritalStatus? MaritalStatus { get; set; }
    public string? Nationality { get; set; }

    public string? PhoneNumber { get; set; }
    public List<int>? ServiceIds { get; set; }
    public int? YearsOfExperience { get; set; }

    // Newly uploaded files to append to the worker's existing documents (not a replace) — see
    // WorkerService.UpdateAsync. Empty = no new documents this update.
    public List<string> DocumentPaths { get; set; } = new();
}
