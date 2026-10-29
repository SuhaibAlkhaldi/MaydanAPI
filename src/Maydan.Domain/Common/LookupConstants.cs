namespace Maydan.Domain.Common;

public static class LookupConstants
{
    public static class ServiceRequestStatus
    {
        public const int PendingWorkerSelection = 7;
        public const int InProgress = 8;
        public const int Completed = 9;
        public const int Cancelled = 10;
        public const int Rejected = 11;
    }

    public static class AttendanceApprovalStatus
    {
        public const int Pending = 12;
        public const int Approved = 13;
        public const int Rejected = 14;
    }

    public static class AttendanceEntryType
    {
        public const int Present = 15;
        public const int Absent = 16;
    }

    public static string ServiceRequestStatusCode(int id) => id switch
    {
        ServiceRequestStatus.PendingWorkerSelection => "PendingWorkerSelection",
        ServiceRequestStatus.InProgress => "InProgress",
        ServiceRequestStatus.Completed => "Completed",
        ServiceRequestStatus.Cancelled => "Cancelled",
        ServiceRequestStatus.Rejected => "Rejected",
        _ => id.ToString()
    };

    public static string AttendanceApprovalStatusCode(int id) => id switch
    {
        AttendanceApprovalStatus.Pending => "Pending",
        AttendanceApprovalStatus.Approved => "Approved",
        AttendanceApprovalStatus.Rejected => "Rejected",
        _ => id.ToString()
    };

    public static string AttendanceEntryTypeCode(int id) => id switch
    {
        AttendanceEntryType.Present => "Present",
        AttendanceEntryType.Absent => "Absent",
        _ => id.ToString()
    };
}