using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

public interface ILookupRepository
{
    Task<List<LookupItem>> GetItemsByCategoryCodeAsync(string categoryCode, CancellationToken cancellationToken = default);
    Task<bool> ExistsInCategoryAsync(string categoryCode, int itemId, CancellationToken cancellationToken = default);
}
