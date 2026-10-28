using Maydan.Application.DTOs.Auth;
using Maydan.Application.DTOs.Common;

namespace Maydan.Application.Interfaces;

public interface IProductionCompanyOnboardingService
{
    Task<ApiResponse<RegisterProductionCompanyResponseDto>> RegisterAsync(RegisterProductionCompanyDto dto, CancellationToken cancellationToken = default);
}
