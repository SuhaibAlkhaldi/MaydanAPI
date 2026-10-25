using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maydan.Domain.Entities
{
    
    // The only period of a given day in which attendance/absence may be recorded for a service request.
    // Defined by the Production House. Stored in UTC.
    
    public class AttendanceTimeWindow
    {
        public int Id { get; set; }

        public int ServiceRequestId { get; set; }
        public ServiceRequest ServiceRequest { get; set; } = null!;

        //Calendar date (date only) the window applies to.
        public DateTime AttendanceDate { get; set; }

        public DateTime OpensAtUtc { get; set; }
        public DateTime ClosesAtUtc { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

}
