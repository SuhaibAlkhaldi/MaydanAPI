using System.Collections.Generic;
using Maydan.Domain.Common;
using Maydan.Domain.Enums;

namespace Maydan.Domain.Entities;

public class Service : SharedEntities
{
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public CalculationType CalculationType { get; set; }
    public decimal Price { get; set; }

    // When true, creating/updating a Worker who provides this service requires at least one
    // uploaded document (see Worker.Documents) — admin-configurable per service, no concept of
    // document "type" or name (see WorkerDocument's own comment on why).
    public bool RequiresDocuments { get; set; }

    // Navigation: Service -> ServiceRequests (one service can be requested multiple times)
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
