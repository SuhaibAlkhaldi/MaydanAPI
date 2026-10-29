namespace Maydan.Domain.Entities;

// Hours a worker worked AFTER his check-out on a given day.
// The money is calculated when the overtime is recorded (snapshot) but it is only counted in the
// worker's total after Bayt Al Urdon approves it.
// One row per worker per day.

public class WorkerOvertime
{
    public int Id { get; set; }

    public int WorkerProjectLinkId { get; set; }
    public WorkerProjectLink WorkerProjectLink { get; set; } = null!;

    // Calendar date (date only) the overtime belongs to.
    public DateTime OvertimeForDate { get; set; }

    // Hours worked after the check-out (min 1, steps of 0.5).
    public decimal Hours { get; set; }

    // ── Pricing snapshot (so later price changes never alter an old overtime) ─
    // Hours covered by one service unit: PerHour = 1, PerShift = 12, PerDay = 9.
    public int HoursPerUnit { get; set; }
    // Wage of one service unit at recording time (WorkerProjectLink.DailyWageAmount)
    public decimal UnitWageSnapshot { get; set; }
    // UnitWageSnapshot x Hours / HoursPerUnit.
    public decimal Amount { get; set; }

    // Who recorded it (Association supervisor or Production House location manager) ─
    public int RecordedByUserId { get; set; }
    public DateTime RecordedDate { get; set; }

    // Bayt Al Urdon decision 
    public int ApprovalStatusId { get; set; }
    public LookupItem ApprovalStatus { get; set; } = null!;
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? RejectionReason { get; set; }

    // Audit 
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
