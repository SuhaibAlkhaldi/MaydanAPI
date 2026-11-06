namespace Maydan.Application.DTOs.Projects;

public class CreateProjectDto
{
    public string ProjectNameEn { get; set; } = string.Empty;
    public string ProjectNameAr { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int ProjectTypeId { get; set; }

    public int ProducerUserId { get; set; }

    public int LocationManagerUserId { get; set; }

    public string WorkPermitImagePath { get; set; } = string.Empty;
    // Example addition to CreateProjectDto & CreateProjectRequest
    public int? ProductionCompanyId { get; set; }
    public int CityId { get; set; }
    public int CityLocationId { get; set; }

    /// <summary>
    /// Optional GPS coordinates forwarded from the API request.
    /// Both must be supplied together; if only one is present, geo-resolution is skipped.
    /// </summary>
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}