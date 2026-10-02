namespace Maydan.Application.DTOs.Projects;

public class ProjectDto
{
    public int Id { get; set; }

    public string ProjectNameEn { get; set; } = string.Empty;
    public string ProjectNameAr { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int ProjectTypeId { get; set; }
    public string ProjectTypeNameEn { get; set; } = string.Empty;
    public string ProjectTypeNameAr { get; set; } = string.Empty;

    public int ProducerUserId { get; set; }
    public string ProducerNameEn { get; set; } = string.Empty;
    public string ProducerNameAr { get; set; } = string.Empty;

    public int LocationManagerUserId { get; set; }
    public string LocationManagerNameEn { get; set; } = string.Empty;
    public string LocationManagerNameAr { get; set; } = string.Empty;

    public string WorkPermitImagePath { get; set; } = string.Empty;

    public int ProductionCompanyId { get; set; }

    public bool IsDeleted { get; set; }
}


    public bool IsDeleted { get; set; }
}
