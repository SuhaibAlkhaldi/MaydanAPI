using Maydan.Domain.Common;
using Maydan.Domain.Enums;

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

    /// <summary>
    /// Time unit for payment calculation (Shift, Day, or Hour).
    /// Shift = 12 hours, Day = 9 hours, Hour = Direct hourly rate.
    /// </summary>
    public ServiceTimeUnit TimeUnit { get; set; } = ServiceTimeUnit.Shift;

    /// <summary>
    /// Duration count: number of shifts/days/hours depending on TimeUnit.
    /// </summary>
    public int ShiftsCount { get; set; }

    // Worker Related Properties
    public int RequestedWorkersCount { get; set; }
    public int SelectedWorkersCount { get; set; } = 0;
    public int AttendanceFrequency { get; set; }
    public string? AdditionalRequirements { get; set; }

    // Financial Related Properties
    /// <summary>
    /// Service price per hour (captured at request creation time).
    /// </summary>
    public decimal UnitPriceSnapshot { get; set; }

    /// <summary>
    /// Expected total payment amount calculated as:
    /// - Shift: (12 * UnitPriceSnapshot) * RequestedWorkersCount * ShiftsCount
    /// - Day: (9 * UnitPriceSnapshot) * RequestedWorkersCount * ShiftsCount
    /// - Hour: (UnitPriceSnapshot * ShiftsCount) * RequestedWorkersCount
    /// </summary>
    public decimal ExpectedTotalAmount { get; set; }

    // Status Related Properties
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.PendingWorkerSelection;

    /// <summary>
    /// Idempotency key to prevent duplicate submissions (check-then-insert).
    /// Combined with ProductionCompanyId in unique database index.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Tracks whether 6-hour reminder notification has been sent.
    /// </summary>
    public bool ReminderSent { get; set; } = false;

    /// <summary>
    /// Timestamp when request was cancelled (soft-delete indicator).
    /// Request is marked as Cancelled status instead of being deleted.
    /// </summary>
    public DateTime? CancelledAt { get; set; }
}
