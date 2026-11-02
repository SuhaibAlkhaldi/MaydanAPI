using Maydan.Domain.Entities;
using NetTopologySuite.Geometries;

namespace Maydan.Application.Interfaces;

public interface IAssociationZoneRepository
{
    Task<AssociationZone?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<AssociationZone>> GetByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the first available (not currently booked) zone whose boundary contains
    /// <paramref name="point"/>, across ALL associations.
    /// Used for "book by geo-tap" flow.
    /// </summary>
    Task<AssociationZone?> FindAvailableContainingAsync(Point point, DateTime start, DateTime end, CancellationToken cancellationToken = default);

    Task AddAsync(AssociationZone zone, CancellationToken cancellationToken = default);
    void Remove(AssociationZone zone);
}

