using Maydan.Domain.Common;

namespace Maydan.Domain.Entities;

public class ServiceRequestWorker : SharedEntities
{
    public int ServiceRequestId { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;

    public int WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;
}