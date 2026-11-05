using Maydan.Domain.Common;
using NetTopologySuite.Geometries;

namespace Maydan.Domain.Entities;

/// <summary>
/// A bookable plot/zone that belongs to a single Association.
/// Its boundary is a WGS-84 Polygon (SRID 4326).
/// Booking is time-scoped: a zone is "booked" for a [StartDate, EndDate] window.
/// </summary>
public class AssociationZone : SharedEntities
{
    public int AssociationId { get; set; }
    public Association Association { get; set; } = null!;

    public string ArabicName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;

    // WGS-84 polygon (SRID 4326) — must lie inside Association.Geofence.
    public Polygon Boundary { get; set; } = null!;

    // null = never booked / available again.
    public DateTime? BookedFrom { get; set; }
    public DateTime? BookedUntil { get; set; }

    // Derived: true when the current moment falls inside [BookedFrom, BookedUntil].
    // Computed in-memory — not a stored column.
    public bool IsBookedNow => BookedFrom.HasValue && BookedUntil.HasValue
        && DateTime.UtcNow >= BookedFrom.Value
        && DateTime.UtcNow <= BookedUntil.Value;

    // Back-link to the ServiceRequest that caused the booking (nullable — zone can
    // be marked booked without a formal service request).
    public int? ServiceRequestId { get; set; }
    public ServiceRequest? ServiceRequest { get; set; }
}

