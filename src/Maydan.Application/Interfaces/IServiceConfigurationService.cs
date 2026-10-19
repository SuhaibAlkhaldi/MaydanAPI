using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Services;

namespace Maydan.Application.Interfaces;

public interface IServiceConfigurationService
{
    Task<ApiResponse<List<ServiceDto>>> GetAllServicesAsync(int currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<ServiceDto>>> GetActiveServicesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<ServiceDto>> GetServiceByIdAsync(int currentUserId, int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<ServiceDto>> CreateServiceAsync(int currentUserId, CreateServiceDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<ServiceDto>> UpdateServiceAsync(int currentUserId, int id, UpdateServiceDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<object?>> DeleteServiceAsync(int currentUserId, int id, CancellationToken cancellationToken = default);
}
