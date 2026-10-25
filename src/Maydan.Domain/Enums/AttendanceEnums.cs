using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maydan.Domain.Enums
{
   

    public enum AttendanceEntryType
    {
        Present = 1,
        Absent = 2
    }

    public enum AttendanceApprovalStatus
    {
        Pending = 1,    // recorded by the Association, waiting for the Production House
        Approved = 2,   // approved by the Production House (or recorded directly by it)
        Rejected = 3    // rejected by the Production House => counts as "not approved"
    }

    //Computed (never stored) outcome of one worker for one calendar day.
    public enum AttendanceDayStatus
    {
        Open = 1,             // recording window still open / not all check-ins recorded yet
        PendingApproval = 2,  // some check-ins wait for Production House approval
        Present = 3,          // every required check-in recorded AND approved
        Absent = 4,           // automatic (or recorded + approved) absence for the whole day
        NotScheduled = 5      // Production House never opened a time window for that day
    }

}
