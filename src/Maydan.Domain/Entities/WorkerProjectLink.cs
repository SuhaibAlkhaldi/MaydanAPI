namespace Maydan.Domain.Entities;

public class WorkerProjectLink
{
    
    public int Id { get; set; }

    // ── Foreign Keys
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

  
    // The service request through which this worker was assigned to the project.
    // Used to derive DailyWageAmount from ServiceRequest → Service.Price.
  
    public int ServiceRequestId { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;

    // ── Audit Columns
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    //UserId (int FK → User.UserId) of whoever created this link. Null when system-generated.
    public int? CreatedBy { get; set; }

    //UserId (int FK → User.UserId) of whoever last updated this link.
    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    
  
    // Daily wage for this worker on this project.
    // Auto-populated from ServiceRequest.Service.Price — NOT manually input.
   
    public decimal DailyWageAmount { get; set; }

    //Whether the worker is still active on this project (soft-deactivation).
    public bool IsWorkerActive { get; set; } = true;
}
