using Maydan.Application.DTOs.Onboarding;
using Maydan.Application.DTOs.UserManagement;

namespace Maydan.Application.DTOs.Associations;

// Field names match workforcment's src/app/features/associations/models/association.model.ts
// exactly (camelCase once System.Text.Json's default policy applies) — the real, already-built
// frontend contract this DTO has to satisfy, not a convention picked here. Country fields are
// surfaced via Association.City.Country (Association itself has no CountryId column — see
// AssociationService's own comment on why CountryId is accepted on write but never persisted).
public class AssociationDto
{
    public int Id { get; set; }
    public string EnglishName { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public string LocationOnGoogleMaps { get; set; } = string.Empty;

    public string Latitude { get; set; } = string.Empty;
    public string Longitude { get; set; } = string.Empty;

    // GeoJSON representation of the association's geofence polygon. Null for legacy associations
    // that pre-date geo support.
    public GeoJsonPolygonDto? Geofence { get; set; }

    public int? CountryId { get; set; }
    public string CountryEnglishName { get; set; } = string.Empty;
    public string CountryArabicName { get; set; } = string.Empty;

    public int? CityId { get; set; }
    public string CityEnglishName { get; set; } = string.Empty;
    public string CityArabicName { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public int WorkersCount { get; set; }

    public List<AssociationZoneDto> Zones { get; set; } = new();
}

/// <summary>GeoJSON Polygon shape accepted on input and returned on output.</summary>
public class GeoJsonPolygonDto
{
    public string Type { get; set; } = "Polygon";

    // Outer ring first, then optional holes.
    // Each coordinate is [longitude, latitude] as per GeoJSON spec (RFC 7946).
    public List<List<double[]>> Coordinates { get; set; } = new();
}

public class AssociationZoneDto
{
    public int Id { get; set; }
    public int AssociationId { get; set; }
    public string? AssociationArabicName { get; set; }
    public string? AssociationEnglishName { get; set; }
    public string ArabicName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;

    // GeoJSON polygon of this bookable zone.
    public GeoJsonPolygonDto Boundary { get; set; } = new();

    public bool IsBookedNow { get; set; }
    public DateTime? BookedFrom { get; set; }
    public DateTime? BookedUntil { get; set; }
    public int? ServiceRequestId { get; set; }
}

public class CreateAssociationDto
{
    public string EnglishName { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public string? LocationOnGoogleMaps { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }

    // GeoJSON polygon for the association's outer geofence.
    public GeoJsonPolygonDto? Geofence { get; set; }

    public int? CountryId { get; set; }
    public int? CityId { get; set; }

    public OnboardAssociationAdminDto? Admin { get; set; }

    // Optional initial zones to create together with the association.
    public List<CreateAssociationZoneDto> Zones { get; set; } = new();
}

public class UpdateAssociationDto : CreateAssociationDto
{
    public int Id { get; set; }
}

/// <summary>Payload to create a new bookable zone inside an association.</summary>
public class CreateAssociationZoneDto
{
    public string EnglishName { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;

    // GeoJSON polygon — must lie inside the association's geofence.
    public GeoJsonPolygonDto Boundary { get; set; } = new();
}

/// <summary>Request body for booking a zone for a time window.</summary>
public class BookZoneDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Optional — attach to an existing ServiceRequest.
    public int? ServiceRequestId { get; set; }

    // Or auto-create a ServiceRequest for this association & zone
    public bool AutoCreateServiceRequest { get; set; } = false;
    public int? ProjectId { get; set; }
    public int? ServiceId { get; set; }
    public int? RequestedWorkersCount { get; set; }
    public int? ShiftsCount { get; set; }
    public int? AttendanceFrequency { get; set; }
    public string? AdditionalRequirements { get; set; }
}

/// <summary>Request body for booking by tapping coordinates on a map.</summary>
public class BookByLocationDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Optional ServiceRequest auto-creation parameters
    public bool AutoCreateServiceRequest { get; set; } = false;
    public int? ProjectId { get; set; }
    public int? ServiceId { get; set; }
    public int? RequestedWorkersCount { get; set; }
    public int? ShiftsCount { get; set; }
    public int? AttendanceFrequency { get; set; }
    public string? AdditionalRequirements { get; set; }
}

public class AssociationDetailsDto : AssociationDto
{
    public List<UserSummaryDto> Users { get; set; } = new();
}
