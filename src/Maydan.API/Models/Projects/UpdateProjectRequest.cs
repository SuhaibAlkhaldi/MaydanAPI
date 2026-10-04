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
}
