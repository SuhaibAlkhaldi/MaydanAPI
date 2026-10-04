using Maydan.Domain.Enums;

namespace Maydan.Application.DTOs.ServiceRequests;

public class CalculationRequestDto
{
    public int ServiceId { get; set; }
    public int RequestedWorkers { get; set; }
    public int DurationCount { get; set; }
    public ServiceTimeUnit TimeUnit { get; set; } = ServiceTimeUnit.Shift;
}
