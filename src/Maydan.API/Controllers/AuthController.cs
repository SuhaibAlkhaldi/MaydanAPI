using Maydan.API.Filters;
using Maydan.Application.DTOs.Auth;
using Maydan.Application.DTOs.Common;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maydan.API.Controllers;

[AllowAnonymous]
[BypassSystemConfigurationGate]
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly IProductionCompanyOnboardingService _productionCompanyOnboardingService;

    public AuthController(IAuthService authService, IProductionCompanyOnboardingService productionCompanyOnboardingService)
    {
        _authService = authService;
        _productionCompanyOnboardingService = productionCompanyOnboardingService;
    }

    [HttpPost("login", Name = "Login User")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost("reset-password", Name = "Reset Password")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.ResetPasswordAsync(dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost("forgot-password", Name = "Forgot Password")]
    public async Task<ActionResult<ApiResponse<ForgotPasswordResponseDto>>> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.ForgotPasswordAsync(dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost("reset-password-with-token", Name = "Reset Password With Token")]
    public async Task<ActionResult<ApiResponse<ResetPasswordWithTokenResponseDto>>> ResetPasswordWithToken([FromBody] ResetPasswordWithTokenDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.ResetPasswordWithTokenAsync(dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost("refresh", Name = "Refresh Access Token")]
    public async Task<ActionResult<ApiResponse<RefreshTokenResponseDto>>> Refresh([FromBody] RefreshTokenRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost("logout", Name = "Logout")]
    public async Task<ActionResult<ApiResponse<LogoutResponseDto>>> Logout([FromBody] LogoutRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LogoutAsync(dto, cancellationToken);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost("register-production-company", Name = "Register Production Company")]
    public async Task<ActionResult<ApiResponse<RegisterProductionCompanyResponseDto>>> RegisterProductionCompany(
         [FromBody] RegisterProductionCompanyDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productionCompanyOnboardingService.RegisterAsync(dto, cancellationToken);
            var response = new ApiResponse<RegisterProductionCompanyResponseDto>(
                success: true,
                messageAr: "تم تسجيل شركة الإنتاج بنجاح.",
                messageEn: "Production company registered successfully.",
                data: result,
                statusCode: 201
            );

            // No GET-by-id endpoint exists for production companies yet (no ProductionCompaniesController
            // at all) — this Location URI is a placeholder for when one is added, not a live route.
            return Created($"api/production-companies/{result.ProductionCompanyId}", response);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }
}