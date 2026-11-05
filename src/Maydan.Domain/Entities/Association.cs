using Maydan.Domain.Common;
using NetTopologySuite.Geometries;

namespace Maydan.Domain.Entities;

public class Association : SharedEntities
{
    public string ArabicName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;

    // CityId is now optional — associations can be located purely by geo boundary.
    public int? CityId { get; set; }
    public City? City { get; set; }

    public string? LocationOnGoogleMaps { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // WGS-84 polygon (SRID 4326) representing the association's outer boundary (geofence).
    public Polygon? Geofence { get; set; }

    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }

    public ICollection<Worker> Workers { get; set; } = new List<Worker>();
    public ICollection<AssociationZone> Zones { get; set; } = new List<AssociationZone>();

    // WorkersCount is intentionally NOT a column here — it's a computed
    // COUNT(Workers WHERE AssociationId = Id), returned via DTO at query time.
}
