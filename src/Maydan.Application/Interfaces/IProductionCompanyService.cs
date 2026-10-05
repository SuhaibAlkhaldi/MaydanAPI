using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.ProductionCompanies;

namespace Maydan.Application.Interfaces;

public interface IProductionCompanyService
{
    Task<ApiResponse<List<ProductionCompanyDto>>> GetAllAsync(int currentUserId, string? search, bool isDeleted, CancellationToken cancellationToken = default);

    Task<ApiResponse<ProductionCompanyDto>> GetByIdAsync(int currentUserId, int productionCompanyId, CancellationToken cancellationToken = default);

    Task<ApiResponse<ProductionCompanyDto>> UpdateStatusAsync(int currentUserId, int productionCompanyId, UpdateProductionCompanyStatusDto dto, CancellationToken cancellationToken = default);
}