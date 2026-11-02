using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Maydan.Infrastructure.Repositories;

public class AssociationZoneRepository : IAssociationZoneRepository
{
    private readonly MaydanDbContext _context;

    public AssociationZoneRepository(MaydanDbContext context)
    {
        _context = context;
    }

    public Task<AssociationZone?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Set<AssociationZone>()
            .Include(z => z.Association)
            .FirstOrDefaultAsync(z => z.Id == id, cancellationToken);

    public Task<List<AssociationZone>> GetByAssociationIdAsync(int associationId, CancellationToken cancellationToken = default) =>
        _context.Set<AssociationZone>()
            .Include(z => z.Association)
            .Where(z => z.AssociationId == associationId)
            .OrderBy(z => z.EnglishName)
            .ToListAsync(cancellationToken);

    public Task<AssociationZone?> FindAvailableContainingAsync(
        Point point,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default) =>
        _context.Set<AssociationZone>()
            .Include(z => z.Association)
            .Where(z =>
                z.Boundary.Contains(point) &&
                // available = never booked, or booking window doesn't overlap requested window
                (z.BookedFrom == null || z.BookedUntil == null ||
                 z.BookedUntil < start || z.BookedFrom > end))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(AssociationZone zone, CancellationToken cancellationToken = default) =>
        await _context.Set<AssociationZone>().AddAsync(zone, cancellationToken);

    public void Remove(AssociationZone zone) =>
        _context.Set<AssociationZone>().Remove(zone);
}

