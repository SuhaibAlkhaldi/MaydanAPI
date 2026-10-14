using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

public class AssociationRepositoryV2 : IAssociationRepositoryV2
{
    private readonly MaydanDbContext _context;

    public AssociationRepositoryV2(MaydanDbContext context)
    {
        _context = context;
    }

    public Task<AssociationV2?> GetByIdAsync(int associationId, CancellationToken cancellationToken = default) =>
        _context.AssociationsV2
            .Include(a => a.City).ThenInclude(c => c.Country)
            .FirstOrDefaultAsync(a => a.Id == associationId, cancellationToken);

    public Task<List<AssociationV2>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.AssociationsV2
            .Include(a => a.City).ThenInclude(c => c.Country)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AssociationV2 association, CancellationToken cancellationToken = default) =>
        await _context.AssociationsV2.AddAsync(association, cancellationToken);

    public void Remove(AssociationV2 association) => _context.AssociationsV2.Remove(association);

    public Task<AssociationV2?> GetByIdIncludingDeletedAsync(int associationId, CancellationToken cancellationToken = default) =>
        _context.AssociationsV2
            .IgnoreQueryFilters()
            .Include(a => a.City).ThenInclude(c => c.Country)
            .FirstOrDefaultAsync(a => a.Id == associationId, cancellationToken);

    public Task<AssociationV2?> GetByIdWithWorkersAsync(int associationId, CancellationToken cancellationToken = default) =>
        _context.AssociationsV2
            .Include(a => a.City).ThenInclude(c => c.Country)
            .Include(a => a.Workers)
            .FirstOrDefaultAsync(a => a.Id == associationId, cancellationToken);

    public async Task<(AssociationV2 AssociationV2, int WorkersCount)?> GetByIdWithWorkersCountAsync(int associationId, CancellationToken cancellationToken = default)
    {
        var result = await _context.AssociationsV2
            .Include(a => a.City).ThenInclude(c => c.Country)
            .Where(a => a.Id == associationId)
            .Select(a => new { AssociationV2 = a, WorkersCount = a.Workers.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        return result is null ? null : (result.AssociationV2, result.WorkersCount);
    }

    public async Task<List<(AssociationV2 AssociationV2, int WorkersCount)>> QueryAsync(
        bool isDeleted,
        string? searchTerm = null,
        bool? orderByWorkersCountAscending = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AssociationV2> query = isDeleted
            ? _context.AssociationsV2.IgnoreQueryFilters().Where(a => a.IsDeleted)
            : _context.AssociationsV2;

        query = query
            .AsNoTracking()
            .Include(a => a.City).ThenInclude(c => c.Country);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(a => a.EnglishName.Contains(term) || a.ArabicName.Contains(term));
        }

        var projected = query.Select(a => new { AssociationV2 = a, WorkersCount = a.Workers.Count() });

        var results = orderByWorkersCountAscending switch
        {
            true => await projected.OrderBy(x => x.WorkersCount).ToListAsync(cancellationToken),
            false => await projected.OrderByDescending(x => x.WorkersCount).ToListAsync(cancellationToken),
            null => await projected.OrderBy(x => x.AssociationV2.EnglishName).ToListAsync(cancellationToken)
        };

        return results.Select(x => (x.AssociationV2, x.WorkersCount)).ToList();
    }

    public async Task<bool> PhoneExistsAsync(string phone, CancellationToken cancellationToken = default) =>
        await _context.AssociationsV2
            .AnyAsync(a => a.ContactPhone == phone, cancellationToken);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        await _context.AssociationsV2
            .AnyAsync(a => a.ContactEmail == email, cancellationToken);
}