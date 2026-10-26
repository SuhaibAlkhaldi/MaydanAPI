using Maydan.API.Filters;
using Maydan.Application.DTOs.Auth;
using Maydan.Application.DTOs.Common;
using Maydan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LoginAsync(dto, cancellationToken);

            return Ok(new ApiResponse<LoginResponseDto>(
                success: true,
                messageAr: "تم تسجيل الدخول بنجاح",
                messageEn: "Login successful",
                data: result
            ));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new ApiResponse<LoginResponseDto>(
                success: false,
                messageAr: "اسم المستخدم أو كلمة المرور غير صحيحة",
                messageEn: exception.Message,
                data: null
            ));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost("reset-password", Name = "Reset Password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.ResetPasswordAsync(dto, cancellationToken);

            return Ok(new ApiResponse<LoginResponseDto>(
                success: true,
                messageAr: "تم إعادة تعيين كلمة المرور بنجاح",
                messageEn: "Password reset successfully",
                data: result
            ));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new ApiResponse<LoginResponseDto>(
                success: false,
                messageAr: "غير مصرح بالوصول",
                messageEn: exception.Message,
                data: null
            ));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost("forgot-password", Name = "Forgot Password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.ForgotPasswordAsync(dto, cancellationToken);

            return Ok(new ApiResponse<ForgotPasswordResponseDto>(
                success: true,
                messageAr: "تم إرسال تعليمات استعادة كلمة المرور",
                messageEn: "Password recovery instructions sent successfully",
                data: result
            ));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost("reset-password-with-token", Name = "Reset Password With Token")]
    public async Task<IActionResult> ResetPasswordWithToken([FromBody] ResetPasswordWithTokenDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.ResetPasswordWithTokenAsync(dto, cancellationToken);

            return Ok(new ApiResponse<ResetPasswordWithTokenResponseDto>(
                success: true,
                messageAr: "تم إعادة تعيين كلمة المرور بنجاح",
                messageEn: "Password reset with token successful",
                data: result
            ));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost("refresh", Name = "Refresh Access Token")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(dto, cancellationToken);

            return Ok(new ApiResponse<RefreshTokenResponseDto>(
                success: true,
                messageAr: "تم تجديد الجلسة بنجاح",
                messageEn: "Token refreshed successfully",
                data: result
            ));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new ApiResponse<RefreshTokenResponseDto>(
                success: false,
                messageAr: "رمز التحديث غير صالح أو منتهي الصلاحية",
                messageEn: exception.Message,
                data: null
            ));
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPost("logout", Name = "Logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LogoutAsync(dto, cancellationToken);

            return Ok(new ApiResponse<LogoutResponseDto>(
                success: true,
                messageAr: "تم تسجيل الخروج بنجاح",
                messageEn: "Logout successful",
                data: result
            ));
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
            if (!TryGetCurrentUserId(out var currentUserId))
            {
             
                return Forbid();
            }
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