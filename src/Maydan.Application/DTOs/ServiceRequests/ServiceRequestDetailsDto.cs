namespace Maydan.Application.DTOs.ServiceRequests;

public class ServiceRequestDetailsDto
{
    public int Id { get; set; }

    // Service
    public int ServiceId { get; set; }
    public string ServiceNameEn { get; set; } = string.Empty;
    public string ServiceNameAr { get; set; } = string.Empty;

    // Project
    public int ProjectId { get; set; }
    public string ProjectNameEn { get; set; } = string.Empty;
    public string ProjectNameAr { get; set; } = string.Empty;

    // Association (chosen by CityId)
    public int AssociationId { get; set; }
    public string AssociationNameEn { get; set; } = string.Empty;
    public string AssociationNameAr { get; set; } = string.Empty;

    // Location coordinates
    public int? CityId { get; set; }
    // Duration
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int ShiftsCount { get; set; }

    // Workers
    public int RequestedWorkersCount { get; set; }
    public int SelectedWorkersCount { get; set; }
    public int AttendanceFrequency { get; set; }

    // Financial snapshot
    public decimal UnitPriceSnapshot { get; set; }
    public decimal ExpectedTotalAmount { get; set; }

    // Status and metadata
    public int StatusId { get; set; }
    public string? StatusNameEn { get; set; }
    public string? StatusNameAr { get; set; }
    public string? RejectionReason { get; set; } 
    public DateTime? CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public string? AdditionalRequirements { get; set; }

    public List<AssignedWorkerDto> AssignedWorkers { get; set; } = new();
}

public class AssignedWorkerDto
{
    public int WorkerId { get; set; }
    public string WorkerNameEn { get; set; } = string.Empty;
    public string WorkerNameAr { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Nationality { get; set; }
    public int? YearsOfExperience { get; set; }

    public List<string> Documents { get; set; }
    public DateTime AssignedAt { get; set; }
    public AssignedWorkerDto(
       int workerId,
       string workerNameEn,
       string? phoneNumber,
       string? nationality,
       int? yearsOfExperience,
       List<string> documents)
    {
        WorkerId = workerId;
        WorkerNameEn = workerNameEn;
        PhoneNumber = phoneNumber;
        Nationality = nationality;
        YearsOfExperience = yearsOfExperience;
        Documents = documents;
    }
}
