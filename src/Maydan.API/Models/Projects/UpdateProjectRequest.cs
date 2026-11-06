using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Maydan.API.Models.Projects;


public class UpdateProjectRequest
{
    [Required]
    public int Id { get; set; }

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

    public IFormFile? WorkPermitImage { get; set; }
    [Required]
    public int CityId { get; set; }

    [Required]
    public int CityLocationId { get; set; }

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double? Longitude { get; set; }
}
