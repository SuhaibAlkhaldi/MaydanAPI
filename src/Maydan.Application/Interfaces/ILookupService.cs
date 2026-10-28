using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Lookups;

namespace Maydan.Application.Interfaces;

public interface ILookupService
{
    Task<ApiResponse<List<LookupItemDto>>> GetByCategoryAsync(string categoryCode, CancellationToken cancellationToken = default);
}
