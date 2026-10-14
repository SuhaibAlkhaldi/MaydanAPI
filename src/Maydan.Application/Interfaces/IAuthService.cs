using Maydan.Application.DTOs.Auth;
using Maydan.Application.DTOs.Common;

namespace Maydan.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default);

    Task<ApiResponse<LoginResponseDto>> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default);

    Task<ApiResponse<ForgotPasswordResponseDto>> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default);

    Task<ApiResponse<ResetPasswordWithTokenResponseDto>> ResetPasswordWithTokenAsync(ResetPasswordWithTokenDto dto, CancellationToken cancellationToken = default);

    Task<ApiResponse<RefreshTokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default);

    Task<ApiResponse<LogoutResponseDto>> LogoutAsync(LogoutRequestDto dto, CancellationToken cancellationToken = default);
}