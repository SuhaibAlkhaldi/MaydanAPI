using Maydan.Domain.Enums;

namespace Maydan.Application.DTOs.Attendance;

//Requests
public class RecordAttendanceDto
{
    public int WorkerProjectLinkId { get; set; }

    //Date the attendance counts for. Defaults to today (Jordan time).
    public DateTime? AttendanceForDate { get; set; }

    public AttendanceEntryType EntryType { get; set; } = AttendanceEntryType.Present;
}

public class RejectAttendanceDto
{
    public string Reason { get; set; } = string.Empty;
}

//Production House sets the daily recording window (local Jordan time, e.g. "08:00:00").
public class SetAttendanceWindowDto
{
    public DateTime FromDate { get; set; }
    public DateTime? ToDate { get; set; }          // omit for a single day
    public TimeSpan OpensAtLocal { get; set; }
    public TimeSpan ClosesAtLocal { get; set; }
}

//Responses
public class AttendanceTimeWindowDto
{
    public DateTime Date { get; set; }
    public TimeSpan OpensAtLocal { get; set; }
    public TimeSpan ClosesAtLocal { get; set; }
    public DateTime OpensAtUtc { get; set; }
    public DateTime ClosesAtUtc { get; set; }
}

public class WorkerAttendanceDto
{
    public int Id { get; set; }
    public int WorkerProjectLinkId { get; set; }
    public DateTime AttendanceForDate { get; set; }
    public int CheckInNumber { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
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
    public AttendanceTimeWindowDto? Window { get; set; }
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

    //PresentDays x DailyWageAmount. Only approved attendance is counted.
    public decimal PayableAmount { get; set; }
}

public class AttendanceSummaryDto
{
    public int ServiceRequestId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int ExpectedCheckInsPerDay { get; set; }
    public int ServiceRequestShiftsCount { get; set; }
    public decimal TotalPayableAmount { get; set; }
    public List<AttendanceSummaryItemDto> Workers { get; set; } = new();
}
