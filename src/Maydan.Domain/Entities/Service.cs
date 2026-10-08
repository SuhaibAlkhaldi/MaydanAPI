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

    // Navigation: Service -> ServiceRequests (one service can be requested multiple times)
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
