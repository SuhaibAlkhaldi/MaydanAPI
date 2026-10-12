namespace Maydan.Application.DTOs.Projects;


public class ProjectQueryDto
{
    public bool IsDeleted { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchByProductionCompanyName { get; set; }
    public int? SearchByProductionCompanyId { get; set; }
}
