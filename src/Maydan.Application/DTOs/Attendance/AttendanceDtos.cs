using Maydan.Domain.Common;

namespace Maydan.Application.DTOs.Attendance;

// Requests 
public class RecordAttendanceDto
{
    //The exact value read from the worker's QR code (Worker.QrCode).
    public string QrCode { get; set; } = string.Empty;

    //The service request the supervisor / location manager is taking attendance for.
    public int ServiceRequestId { get; set; }

    //Date the attendance counts for. Defaults to today (Jordan time).
    public DateTime? AttendanceForDate { get; set; }

    public int EntryTypeId { get; set; } = LookupConstants.AttendanceEntryType.Present;
}

public class RejectAttendanceDto
{
    public string Reason { get; set; } = string.Empty;
}

//One scan window of the day (local Jordan time, e.g. "08:00:00").
public class AttendanceSlotDto
{
    public TimeSpan OpensAtLocal { get; set; }
    public TimeSpan ClosesAtLocal { get; set; }
}


// Production House defines the scan windows of each day. The number of slots must equal the
// service request's AttendanceFrequency. Same slots are applied to every day from FromDate to ToDate.
public class SetAttendanceWindowDto
{
    public DateTime FromDate { get; set; }
    public DateTime? ToDate { get; set; }          // omit for a single day
    public List<AttendanceSlotDto> Slots { get; set; } = new();
}

// Responses 
public class AttendanceTimeWindowDto
{
    public DateTime Date { get; set; }
    public int SlotNumber { get; set; }
    public TimeSpan OpensAtLocal { get; set; }
    public TimeSpan ClosesAtLocal { get; set; }
    public DateTime OpensAtUtc { get; set; }
    public DateTime ClosesAtUtc { get; set; }
}

public class WorkerAttendanceDto
{
    public int Id { get; set; }
    public int WorkerProjectLinkId { get; set; }
    public int WorkerId { get; set; }
    public string? WorkerName { get; set; }
    public DateTime AttendanceForDate { get; set; }
    public int CheckInNumber { get; set; }
    public int EntryTypeId { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public int ApprovalStatusId { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public string? EntryTypeNameEn { get; set; }
    public string? EntryTypeNameAr { get; set; }
    public string? ApprovalStatusNameEn { get; set; }
    public string? ApprovalStatusNameAr { get; set; }
    public int CityId { get; set; }

    public int? AssociationUserId { get; set; }
    public DateTime? AssociationCheckInDate { get; set; }
    public DateTime? AssociationCheckOutDate { get; set; }

    public int? ProductionCompanyUserId { get; set; }
    public DateTime? ProductionCompanyCheckInDate { get; set; }
    public DateTime? ProductionCompanyCheckOutDate { get; set; }

    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? RejectionReason { get; set; }
   
}

public class DayRosterItemDto
{
    public int WorkerProjectLinkId { get; set; }
    public int WorkerId { get; set; }
    public string? WorkerName { get; set; }
    public string DayStatus { get; set; } = string.Empty;
    public int ExpectedCheckIns { get; set; }
    public int RecordedCheckIns { get; set; }
    public int ApprovedCheckIns { get; set; }
    public int PendingCheckIns { get; set; }
    public List<WorkerAttendanceDto> Entries { get; set; } = new();
}

public class DayRosterDto
{
    public int ServiceRequestId { get; set; }
    public DateTime Date { get; set; }
    public int ExpectedCheckInsPerDay { get; set; }
    public int? CurrentSlotNumber { get; set; }     // the slot open right now (null if none)
    public List<AttendanceTimeWindowDto> Slots { get; set; } = new();
    public List<DayRosterItemDto> Workers { get; set; } = new();
}

public class AttendanceSummaryItemDto
{
    public int WorkerProjectLinkId { get; set; }
    public int WorkerId { get; set; }
    public string? WorkerName { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int PendingApprovalDays { get; set; }
    public int OpenDays { get; set; }
    public int NotScheduledDays { get; set; }
    public decimal DailyWageAmount { get; set; }

    //PresentDays x DailyWageAmount (approved attendance only).
    public decimal AttendanceAmount { get; set; }

    //Overtime APPROVED by Bayt Al Urdon on days the worker was Present. Counted in PayableAmount.
    public decimal ApprovedOvertimeHours { get; set; }
    public decimal ApprovedOvertimeAmount { get; set; }

    //Overtime still waiting for Bayt Al Urdon. NOT counted in PayableAmount.
    public decimal PendingOvertimeAmount { get; set; }

    //AttendanceAmount + ApprovedOvertimeAmount.
    public decimal PayableAmount { get; set; }
}

public class AttendanceSummaryDto
{
    public int ServiceRequestId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int ExpectedCheckInsPerDay { get; set; }
    public int ServiceRequestShiftsCount { get; set; }
    public decimal TotalApprovedOvertimeAmount { get; set; }
    public decimal TotalPendingOvertimeAmount { get; set; }
    public decimal TotalPayableAmount { get; set; }
    public List<AttendanceSummaryItemDto> Workers { get; set; } = new();
}

//Overtime 
public class RecordOvertimeDto
{
    //The exact value read from the worker's QR code (Worker.QrCode).
    public string QrCode { get; set; } = string.Empty;
    public int ServiceRequestId { get; set; }

    //The day the overtime belongs to. Defaults to today (Jordan time).
    public DateTime? AttendanceForDate { get; set; }

    //Hours worked after the check-out: at least 1, in steps of 0.5.
    public decimal Hours { get; set; }
}

public class WorkerOvertimeDto
{
    public int Id { get; set; }
    public int WorkerProjectLinkId { get; set; }
    public int WorkerId { get; set; }
    public string? WorkerName { get; set; }
    public int ProjectId { get; set; }
    public int ServiceRequestId { get; set; }
    public DateTime OvertimeForDate { get; set; }
    public decimal Hours { get; set; }
    public int HoursPerUnit { get; set; }
    public decimal UnitWageAmount { get; set; }
    public decimal Amount { get; set; }
    public int ApprovalStatusId { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;

    public int RecordedByUserId { get; set; }
    public DateTime RecordedDate { get; set; }
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? RejectionReason { get; set; }
    public string? ApprovalStatusNameEn { get; set; }
    public string? ApprovalStatusNameAr { get; set; }
}
