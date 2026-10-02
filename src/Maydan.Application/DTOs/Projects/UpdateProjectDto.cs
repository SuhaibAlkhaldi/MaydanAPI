namespace Maydan.Application.DTOs.Projects;

public class UpdateProjectDto
{
    public string ProjectNameEn { get; set; } = string.Empty;
    public string ProjectNameAr { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int ProjectTypeId { get; set; }

    public int ProducerUserId { get; set; }

    public int LocationManagerUserId { get; set; }


    public string? WorkPermitImagePath { get; set; }
}
