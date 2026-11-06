using Maydan.Application.DTOs.Onboarding;
using Maydan.Application.DTOs.UserManagement;

namespace Maydan.Application.DTOs.Associations;

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

    public int? CityId { get; set; }
    public string CityEnglishName { get; set; } = string.Empty;
    public string CityArabicName { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public int WorkersCount { get; set; }
}

/// <summary>GeoJSON Polygon shape accepted on input and returned on output.</summary>
public class GeoJsonPolygonDto
{
    public string Type { get; set; } = "Polygon";

    // Outer ring first, then optional holes.
    // Each coordinate is [longitude, latitude] as per GeoJSON spec (RFC 7946).
    public List<List<double[]>> Coordinates { get; set; } = new();
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
    public int? CityId { get; set; }

    public OnboardAssociationAdminDto? Admin { get; set; }
}

public class UpdateAssociationDto : CreateAssociationDto
{
    public int Id { get; set; }
}

public class AssociationDetailsDto : AssociationDto
{
    public List<UserSummaryDto> Users { get; set; } = new();
}
