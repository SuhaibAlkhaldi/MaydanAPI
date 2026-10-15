using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maydan.Domain.Enums
{
    public enum ServiceRequestStatus
    {
    PendingWorkerSelection = 1,
    InProgress = 2,             
    Completed = 3,              
    Cancelled = 4,              
    Rejected = 5
    }
}
