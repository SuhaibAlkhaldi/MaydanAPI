using Maydan.Domain.Common;

namespace Maydan.Domain.Entities;

public class Project : SharedEntities
{
    public string ProjectNameEn { get; set; } = string.Empty;
    public string ProjectNameAr { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int ProjectTypeId { get; set; }
    public ProjectType ProjectType { get; set; } = null!;

    public int ProducerUserId { get; set; }
    public User Producer { get; set; } = null!;

    public int LocationManagerUserId { get; set; }
    public User LocationManager { get; set; } = null!;

    public string WorkPermitImagePath { get; set; } = string.Empty;

    public int ProductionCompanyId { get; set; }
    public ProductionCompany ProductionCompany { get; set; } = null!;

    // Approval/status remains handled by the Service Request workflow.

    public int CityId { get; set; }
    public City City { get; set; } = null!;

    public int CityLocationId { get; set; }
    public CityLocation CityLocation { get; set; } = null!;
}