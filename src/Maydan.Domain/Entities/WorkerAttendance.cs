using Maydan.Domain.Enums;

namespace Maydan.Domain.Entities;

//One attendance "check-in slot" of one worker on one day.
//A service with AttendanceFrequency = N produces up to N rows per worker per day
//(CheckInNumber 1..N). Association records -> Production House approves each row.

public class WorkerAttendance
{
    public int Id { get; set; }

    public int WorkerProjectLinkId { get; set; }
    public WorkerProjectLink WorkerProjectLink { get; set; } = null!;

    //Calendar date the attendance counts for (date only).
    public DateTime AttendanceForDate { get; set; }

    //1..ServiceRequest.AttendanceFrequency
    public int CheckInNumber { get; set; }

    public AttendanceEntryType EntryType { get; set; } = AttendanceEntryType.Present;

    //Snapshot of the service request location (City of the request's Association).
    public int CityId { get; set; }
    public City City { get; set; } = null!;

    //Association side 
    public int? AssociationUserId { get; set; }
    public DateTime? AssociationCheckInDate { get; set; }
    public DateTime? AssociationCheckOutDate { get; set; }

    //Production House side (direct recording) 
    public int? ProductionCompanyUserId { get; set; }
    public DateTime? ProductionCompanyCheckInDate { get; set; }
    public DateTime? ProductionCompanyCheckOutDate { get; set; }

    //Approval by the Production House 
    public AttendanceApprovalStatus ApprovalStatus { get; set; } = AttendanceApprovalStatus.Pending;
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? RejectionReason { get; set; }

    //Audit 
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
