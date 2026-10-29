namespace Maydan.Application.DTOs.ServiceRequests;

public class ServiceRequestDto
{
    public int Id { get; set; }
    public int StatusId { get; set; }
    public string? StatusNameEn { get; set; }
    public string? StatusNameAr { get; set; }
}
