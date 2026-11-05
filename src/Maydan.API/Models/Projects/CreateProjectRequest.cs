using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Maydan.API.Models.Projects;

public class CreateProjectRequest
{
    [Required]
    [MaxLength(200)]
    public string ProjectNameEn { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ProjectNameAr { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public int ProjectTypeId { get; set; }

    [Required]
    public int ProducerUserId { get; set; }

    [Required]
    public int LocationManagerUserId { get; set; }

    [Required]
    public IFormFile WorkPermitImage { get; set; } = null!;
    [Required]
    public int CityId { get; set; }

    [Required]
    public int CityLocationId { get; set; }

    /// <summary>
    /// Optional GPS coordinates of the filming location.
    /// When provided, the system will attempt to geo-resolve the responsible
    /// Association via its Geofence before falling back to city matching.
    /// </summary>
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double? Longitude { get; set; }
}