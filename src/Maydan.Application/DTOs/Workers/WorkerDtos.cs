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
    // Required only when the caller is Bayt-AlUrdon (must pick a target association explicitly);
    // ignored for an Association-role caller, who always registers under their own entity.
    public int? AssociationId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string CivilId { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public MaritalStatus MaritalStatus { get; set; }
    public string Nationality { get; set; } = string.Empty;
    public int CountryId { get; set; }
    public int CityId { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;
    public List<int> ServiceIds { get; set; } = new();
    public int YearsOfExperience { get; set; }
}

// Deliberately has no AssociationId and no CivilId — both are permanent once the worker is
// created (see WorkerService's own comment on why), so the field simply doesn't exist on this
// DTO rather than existing-but-ignored.
public class UpdateWorkerDto
{
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public MaritalStatus MaritalStatus { get; set; }
    public string Nationality { get; set; } = string.Empty;
    public int CountryId { get; set; }
    public int CityId { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;
    public List<int> ServiceIds { get; set; } = new();
    public int YearsOfExperience { get; set; }
}
