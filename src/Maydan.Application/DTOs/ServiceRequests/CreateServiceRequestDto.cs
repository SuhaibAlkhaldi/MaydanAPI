using System;
using Maydan.Domain.Enums;

namespace Maydan.Application.DTOs.ServiceRequests;

public class CreateServiceRequestDto
{
    public int ProjectId { get; set; }
    public int ServiceId { get; set; }
    public int CityId { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ServiceTimeUnit TimeUnit { get; set; } = ServiceTimeUnit.Shift;
    public int ShiftsCount { get; set; }

    public int RequestedWorkersCount { get; set; }
    public int AttendanceFrequency { get; set; }
    public string? AdditionalRequirements { get; set; }

    // Coordinates for the requested location (optional). If not provided, association coordinates will be used when available.
    //public decimal? Latitude { get; set; }
    //public decimal? Longitude { get; set; }

    public decimal UnitPriceSnapshot { get; set; }
    public decimal ExpectedTotalAmount { get; set; }

    public string? IdempotencyKey { get; set; }
}
