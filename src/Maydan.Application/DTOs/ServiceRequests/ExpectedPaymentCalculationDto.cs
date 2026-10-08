namespace Maydan.Application.DTOs.ServiceRequests;

public class ExpectedPaymentCalculationDto
{
    public decimal UnitPrice { get; set; }
    public int RequestedWorkers { get; set; }
    public int DurationUnits { get; set; }
    public decimal TotalExpectedPayment { get; set; }
    public string FormattedMessage { get; set; } = string.Empty;
}
