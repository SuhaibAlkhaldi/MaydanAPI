using Maydan.Domain.Common;

namespace Maydan.Domain.Entities;

public class ServiceRequest : SharedEntities
{
    // Navigation & Foreign Keys
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int ProductionCompanyId { get; set; }
    public ProductionCompany ProductionCompany { get; set; } = null!;

    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public int AssociationId { get; set; }
    public Association Association { get; set; } = null!;

    // Duration and Time Related Properties
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int ShiftsCount { get; set; }

    // Worker Related Properties
    public int RequestedWorkersCount { get; set; }
    public int SelectedWorkersCount { get; set; } = 0;
    public int AttendanceFrequency { get; set; }
    public string? AdditionalRequirements { get; set; }

    // -----------Financial Related Properties-----------
    public decimal UnitPriceSnapshot { get; set; }
    public decimal ExpectedTotalAmount { get; set; }

    // Status Related Properties
    public int StatusId { get; set; }
    public LookupItem Status { get; set; } = null!;

    // Tracks whether 6-hour reminder notification has been sent.
    public bool ReminderSent { get; set; } = false;

    // Timestamp when request was cancelled (soft-delete indicator).
    public DateTime? CancelledAt { get; set; }
    // Navigation to assigned workers
    public ICollection<ServiceRequestWorker> ServiceRequestWorkers { get; set; } = new HashSet<ServiceRequestWorker>();

    // RejectionReason
    public string? RejectionReason { get; set; }
}
