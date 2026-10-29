using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maydan.Domain.Enums
{
   

   
  

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
