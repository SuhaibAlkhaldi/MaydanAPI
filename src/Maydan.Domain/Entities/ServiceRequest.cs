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

    
    /// Time unit for payment calculation (Shift, Day, or Hour).
    /// Shift = 12 hours, Day = 9 hours, Hour = Direct hourly rate.
    
    //public ServiceTimeUnit TimeUnit { get; set; } = ServiceTimeUnit.Shift;

   
    /// Duration count: number of shifts/days/hours depending on TimeUnit.
  
    public int ShiftsCount { get; set; }
    //DurationCount 


    // Worker Related Properties
    public int RequestedWorkersCount { get; set; }
    public int SelectedWorkersCount { get; set; } = 0;
    public int AttendanceFrequency { get; set; }
    public string? AdditionalRequirements { get; set; }

    // Financial Related Properties
    
    /// Service price per hour (captured at request creation time).
    
    public decimal UnitPriceSnapshot { get; set; }

  
    
    public decimal ExpectedTotalAmount { get; set; }

    // Status Related Properties
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.PendingWorkerSelection;




   
    /// Tracks whether 6-hour reminder notification has been sent.
   
    public bool ReminderSent { get; set; } = false;

   
    /// Timestamp when request was cancelled (soft-delete indicator).
    /// Request is marked as Cancelled status instead of being deleted.
   
    public DateTime? CancelledAt { get; set; }
    // Navigation to assigned workers
    public ICollection<ServiceRequestWorker> ServiceRequestWorkers { get; set; } = new HashSet<ServiceRequestWorker>();

    //RejectionReason
    public string? RejectionReason { get; set; }

}
