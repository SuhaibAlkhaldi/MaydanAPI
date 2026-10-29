using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

public class LookupRepository : ILookupRepository
{
    private readonly MaydanDbContext _context;

    public LookupRepository(MaydanDbContext context)
    {
        _context = context;
    }

    public Task<List<LookupItem>> GetItemsByCategoryCodeAsync(string categoryCode, CancellationToken cancellationToken = default) =>
        _context.LookupItems
            .AsNoTracking()
            .Where(i => i.IsActive && i.Category.Code == categoryCode)
            .OrderBy(i => i.SortOrder)
            .Select(i => new LookupItem
            {
                Id = i.Id,
                CategoryId = i.CategoryId,
                Code = i.Code,
                ArabicName = i.ArabicName,
                EnglishName = i.EnglishName,
                SortOrder = i.SortOrder
            })
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsInCategoryAsync(string categoryCode, int itemId, CancellationToken cancellationToken = default) =>
        _context.LookupItems
            .AnyAsync(i => i.Id == itemId && i.IsActive && i.Category.Code == categoryCode, cancellationToken);
}
