namespace Maydan.Application.DTOs.ProjectDetails;

public class ProjectDetailsDto
{
    public ProjectDetailsInfoDto Project { get; set; } = new();
    public ProjectDetailsPersonDto? Producer { get; set; }
    public ProjectDetailsPersonDto? LocationManager { get; set; }
    public List<ProjectDetailsSupervisorDto> AssociationSupervisors { get; set; } = new();
    public List<ProjectDetailsWorkerDto> Workers { get; set; } = new();
    public List<ProjectDetailsServiceRequestDto> ServiceRequests { get; set; } = new();
}

public class ProjectDetailsInfoDto
{
    public int Id { get; set; }
    public string? ProjectNameEn { get; set; }
    public string? ProjectNameAr { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ProjectType { get; set; }
    public string? WorkPermitImagePath { get; set; }
    public int? ProductionCompanyId { get; set; }
    public string? ProductionCompanyName { get; set; }
}

public class ProjectDetailsPersonDto
{
    public int? UserId { get; set; }
    public string? FullNameEn { get; set; }
    public string? FullNameAr { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

public class ProjectDetailsSupervisorDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? FullNameEn { get; set; }
    public string? FullNameAr { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

//One row per worker per service request (a worker on two requests appears twice).
public class ProjectDetailsWorkerDto
{
    public int WorkerProjectLinkId { get; set; }
    public int WorkerId { get; set; }
    public string? WorkerFullName { get; set; }
    public string? CivilId { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsWorkerActive { get; set; }

    //Daily wage = the service price at the time the worker was assigned.
    public decimal DailyWageAmount { get; set; }

    public int ServiceRequestId { get; set; }
    public string? ServiceNameEn { get; set; }
    public string? ServiceNameAr { get; set; }
    public decimal? ServicePrice { get; set; }
    public string? AssociationName { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? ServiceStartDate { get; set; }
    public DateTime? ServiceEndDate { get; set; }
    public string? ServiceRequestStatus { get; set; }

    /// True only while the worker is still on duty for this request (in progress and not past its end date).
    public bool IsCurrent { get; set; }
}

public class ProjectDetailsServiceRequestDto
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public string? ServiceNameEn { get; set; }
    public string? ServiceNameAr { get; set; }
    public int AssociationId { get; set; }
    //public string? AssociationName { get; set; }
    public string AssociationNameEn { get; set; } = string.Empty;
    public string AssociationNameAr { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int ShiftsCount { get; set; }
    public int RequestedWorkersCount { get; set; }
    public int SelectedWorkersCount { get; set; }
    public int AttendanceFrequency { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public decimal ExpectedTotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AdditionalRequirements { get; set; }
    public string? RejectionReason { get; set; }
    public List<ProjectDetailsAssignedWorkerDto> AssignedWorkers { get; set; } = new();
}

public class ProjectDetailsAssignedWorkerDto
{
    public int WorkerId { get; set; }
    public string? WorkerName { get; set; }
}
