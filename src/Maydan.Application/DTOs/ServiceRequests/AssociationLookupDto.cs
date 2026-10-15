namespace Maydan.Application.DTOs.ServiceRequests;

public class AssociationLookupDto
{
    public int AssociationId { get; set; }
    public string EnglishName { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public int CityId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
