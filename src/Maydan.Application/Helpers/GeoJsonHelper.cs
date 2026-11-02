using Maydan.Application.DTOs.Associations;
using NetTopologySuite.Geometries;

namespace Maydan.Application.Helpers;

/// <summary>
/// Converts between the GeoJsonPolygonDto wire format and NetTopologySuite Polygon objects.
/// All coordinates follow GeoJSON spec (RFC 7946): [longitude, latitude], SRID 4326.
/// </summary>
public static class GeoJsonHelper
{
    private static readonly GeometryFactory Factory = new(new PrecisionModel(), 4326);

    public static Polygon ToPolygon(GeoJsonPolygonDto dto)
    {
        if (dto.Coordinates == null || dto.Coordinates.Count == 0)
            throw new ArgumentException("Geofence must have at least one ring.");

        var shell = ToLinearRing(dto.Coordinates[0]);

        var holes = dto.Coordinates
            .Skip(1)
            .Select(ToLinearRing)
            .ToArray();

        return Factory.CreatePolygon(shell, holes);
    }

    public static GeoJsonPolygonDto FromPolygon(Polygon polygon) => new()
    {
        Type = "Polygon",
        Coordinates = new List<List<double[]>>
        {
            polygon.ExteriorRing.Coordinates.Select(c => new[] { c.X, c.Y }).ToList()
        }
        .Concat(Enumerable.Range(0, polygon.NumInteriorRings)
            .Select(i => polygon.GetInteriorRingN(i).Coordinates
                .Select(c => new[] { c.X, c.Y }).ToList()))
        .ToList()
    };

    private static LinearRing ToLinearRing(List<double[]> ring)
    {
        if (ring.Count < 4)
            throw new ArgumentException("Each polygon ring must have at least 4 coordinate pairs (first and last must match).");

        var coords = ring.Select(c => new Coordinate(c[0], c[1])).ToArray();

        // Auto-close the ring if the caller didn't.
        if (!coords[0].Equals(coords[^1]))
            coords = coords.Append(coords[0]).ToArray();

        return Factory.CreateLinearRing(coords);
    }
}

