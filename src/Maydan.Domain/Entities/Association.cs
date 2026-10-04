using Maydan.Domain.Common;

namespace Maydan.Domain.Entities;

public class Association : SharedEntities
{
    public string ArabicName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;

    public int CityId { get; set; }
    public City City { get; set; } = null!;

    public string? LocationOnGoogleMaps { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }

    public ICollection<Worker> Workers { get; set; } = new List<Worker>();

    // WorkersCount is intentionally NOT a column here — it's a computed
    // COUNT(Workers WHERE AssociationId = Id), returned via DTO at query time.
}
