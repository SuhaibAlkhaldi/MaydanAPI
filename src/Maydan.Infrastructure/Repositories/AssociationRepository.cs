using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

public class AssociationRepository : IAssociationRepository
{
    private readonly MaydanDbContext _context;

    public AssociationRepository(MaydanDbContext context)
    {
        _context = context;
    }

    public Task<Association?> GetByIdAsync(int associationId, CancellationToken cancellationToken = default) =>
        _context.Associations
            .Include(a => a.City).ThenInclude(c => c.Country)
            .FirstOrDefaultAsync(a => a.Id == associationId, cancellationToken);

    public Task<List<Association>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.Associations
            .Include(a => a.City).ThenInclude(c => c.Country)
            .ToListAsync(cancellationToken);

    public Task<Association?> GetByCityIdAsync(int cityId, CancellationToken cancellationToken = default) =>
        _context.Associations
            .Include(a => a.City).ThenInclude(c => c.Country)
            .FirstOrDefaultAsync(a => a.CityId == cityId, cancellationToken);

    public async Task AddAsync(Association association, CancellationToken cancellationToken = default) =>
        await _context.Associations.AddAsync(association, cancellationToken);

    public void Remove(Association association) => _context.Associations.Remove(association);

    public Task<Association?> GetByIdIncludingDeletedAsync(int associationId, CancellationToken cancellationToken = default) =>
        _context.Associations
            .IgnoreQueryFilters()
            .Include(a => a.City).ThenInclude(c => c.Country)
            .FirstOrDefaultAsync(a => a.Id == associationId, cancellationToken);

    public Task<Association?> GetByIdWithWorkersAsync(int associationId, CancellationToken cancellationToken = default) =>
        _context.Associations
            .Include(a => a.City).ThenInclude(c => c.Country)
            .Include(a => a.Workers)
            .FirstOrDefaultAsync(a => a.Id == associationId, cancellationToken);

    public async Task<(Association Association, int WorkersCount)?> GetByIdWithWorkersCountAsync(int associationId, CancellationToken cancellationToken = default)
    {
        var result = await _context.Associations
            .Include(a => a.City).ThenInclude(c => c.Country)
            .Where(a => a.Id == associationId)
            .Select(a => new { Association = a, WorkersCount = a.Workers.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        return result is null ? null : (result.Association, result.WorkersCount);
    }

    public async Task<List<(Association Association, int WorkersCount)>> QueryAsync(
        bool isDeleted,
        string? searchTerm = null,
        bool? orderByWorkersCountAscending = null,
        CancellationToken cancellationToken = default)
    {
        // Same "global filter already excludes deleted rows for the active view, only the deleted
        // view needs to explicitly bypass it" shape as ProjectRepository.GetAllAsync's own comment.
        // Workers.Count() below is a SEPARATE entity with its own independent soft-delete filter —
        // IgnoreQueryFilters() here scopes only to Associations, so a deleted association's worker
        // count still correctly excludes that association's own deleted workers.
        IQueryable<Association> query = isDeleted
            ? _context.Associations.IgnoreQueryFilters().Where(a => a.IsDeleted)
            : _context.Associations;

        query = query
            .AsNoTracking()
            .Include(a => a.City).ThenInclude(c => c.Country);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(a => a.EnglishName.Contains(term) || a.ArabicName.Contains(term));
        }

        var projected = query.Select(a => new { Association = a, WorkersCount = a.Workers.Count() });

        var results = orderByWorkersCountAscending switch
        {
            true => await projected.OrderBy(x => x.WorkersCount).ToListAsync(cancellationToken),
            false => await projected.OrderByDescending(x => x.WorkersCount).ToListAsync(cancellationToken),
            null => await projected.OrderBy(x => x.Association.EnglishName).ToListAsync(cancellationToken)
        };

        return results.Select(x => (x.Association, x.WorkersCount)).ToList();
    }
}
