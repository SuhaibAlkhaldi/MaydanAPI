using Maydan.Domain.Enums;

namespace Maydan.Application.DTOs.ServiceRequests;

public class ServiceRequestDto
{
    public int Id { get; set; }
    public ServiceRequestStatus Status { get; set; }
}
