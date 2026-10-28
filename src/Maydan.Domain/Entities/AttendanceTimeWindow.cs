namespace Maydan.Domain.Entities;

/// <summary>
/// The only period of a given day in which attendance/absence may be recorded for a service request.
/// Defined by the Production House. Stored in UTC.
/// </summary>
public class AttendanceTimeWindow
{
    public int Id { get; set; }

    public int ServiceRequestId { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;

    /// <summary>Calendar date (date only) the window applies to.</summary>
    public DateTime AttendanceDate { get; set; }

    /// <summary>1..AttendanceFrequency. One slot = one scan window = exactly one scan per worker.</summary>
    public int SlotNumber { get; set; }

    public DateTime OpensAtUtc { get; set; }
    public DateTime ClosesAtUtc { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
