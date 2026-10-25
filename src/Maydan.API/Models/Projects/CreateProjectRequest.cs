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
}