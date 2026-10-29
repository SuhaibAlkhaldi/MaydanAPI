using Maydan.Application.Common;
using Maydan.Application.DTOs.Auth;
using Maydan.Application.DTOs.Common;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using System.Security.Cryptography;

namespace Maydan.Application.Services;

public class AuthService : IAuthService
{
    private const int ResetTokenValidityHours = 24;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmailSender _emailSender;
    private readonly IFrontendLinkBuilder _frontendLinkBuilder;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IEmailSender emailSender,
        IFrontendLinkBuilder frontendLinkBuilder)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailSender = emailSender;
        _frontendLinkBuilder = frontendLinkBuilder;
    }

    public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return Fail<LoginResponseDto>("البريد الإلكتروني وكلمة المرور مطلوبان.", "Email and password are required.");
        }

        var email = dto.Email.Trim();
        var user = await _unitOfWork.Users.GetByEmailWithAccessAsync(email, cancellationToken);

        if (user is null || !user.IsActive || !_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            return Fail<LoginResponseDto>("البريد الإلكتروني أو كلمة المرور غير صحيحة.", "Invalid email or password.");
        }

        var authUser = MapAuthUser(user);

        if (user.MustResetPassword)
        {
            var resetRequiredResponse = new LoginResponseDto(
                false,
                true,
                null,
                null,
                authUser,
                "Password reset is required before accessing the system.");
            return Ok(resetRequiredResponse, "يجب إعادة تعيين كلمة المرور قبل الوصول إلى النظام.", "Password reset is required before accessing the system.");
        }

        var token = _jwtTokenGenerator.GenerateAccessToken(authUser);

        string? refreshToken = null;
        DateTime? refreshTokenExpiresAtUtc = null;

        if (dto.RememberMe)
        {
            (refreshToken, refreshTokenExpiresAtUtc) = await IssueRefreshTokenAsync(user.UserId, cancellationToken);
        }

        var loginResponse = new LoginResponseDto(
            true,
            false,
            token.AccessToken,
            token.ExpiresAtUtc,
            authUser,
            "Login successful.",
            refreshToken,
            refreshTokenExpiresAtUtc);

        return Ok(loginResponse, "تم تسجيل الدخول بنجاح.", "Login successful.");
    }

    public async Task<ApiResponse<LoginResponseDto>> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            return Fail<LoginResponseDto>("البريد الإلكتروني، كلمة المرور الحالية، وكلمة المرور الجديدة مطلوبان.", "Email, current password, and new password are required.");
        }

        var email = dto.Email.Trim();
        var user = await _unitOfWork.Users.GetByEmailWithAccessAsync(email, cancellationToken);

        if (user is null || !user.IsActive || !_passwordHasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
        {
            return Fail<LoginResponseDto>("البريد الإلكتروني أو كلمة المرور غير صحيحة.", "Invalid email or password.");
        }

        var trackedUser = await _unitOfWork.Users.GetByIdAsync(user.UserId, cancellationToken);
        if (trackedUser is null)
        {
            return Fail<LoginResponseDto>("البريد الإلكتروني أو كلمة المرور غير صحيحة.", "Invalid email or password.");
        }

        trackedUser.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
        trackedUser.MustResetPassword = false;
        await RevokeAllActiveRefreshTokensAsync(trackedUser.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var authUser = MapAuthUser(user);
        var token = _jwtTokenGenerator.GenerateAccessToken(authUser);

        var resetResponse = new LoginResponseDto(
            true,
            false,
            token.AccessToken,
            token.ExpiresAtUtc,
            authUser,
            "Password reset successful.");

        return Ok(resetResponse, "تم إعادة تعيين كلمة المرور بنجاح.", "Password reset successful.");
    }

    public async Task<ApiResponse<ForgotPasswordResponseDto>> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return Fail<ForgotPasswordResponseDto>("البريد الإلكتروني مطلوب.", "Email is required.");
        }

        const string genericMessageAr = "إذا كان الحساب المرتبط بهذا البريد موجوداً، فقد تم إرسال رابط إعادة تعيين كلمة المرور إليه.";
        const string genericMessageEn = "If an account with that email exists, a password reset link has been sent to it.";

        var email = dto.Email.Trim();
        var user = await _unitOfWork.Users.GetByEmailWithAccessAsync(email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Ok(new ForgotPasswordResponseDto(genericMessageEn), genericMessageAr, genericMessageEn);
        }

        var outstandingTokens = await _unitOfWork.PasswordResetTokens.GetAllAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var outstanding in outstandingTokens.Where(t => t.UserId == user.UserId && !t.IsUsed && t.ExpiresAtUtc >= now))
        {
            outstanding.IsUsed = true;
            outstanding.UsedAtUtc = now;
        }

        var rawToken = GenerateRawResetToken();
        var resetToken = new PasswordResetToken
        {
            UserId = user.UserId,
            TokenHash = _passwordHasher.HashPassword(rawToken),
            ExpiresAtUtc = DateTime.UtcNow.AddHours(ResetTokenValidityHours),
            IsUsed = false
        };

        await _unitOfWork.PasswordResetTokens.AddAsync(resetToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var resetLink = _frontendLinkBuilder.BuildResetPasswordLink(rawToken);
        var bodyHtml = $"""
            <p>Hi {System.Net.WebUtility.HtmlEncode(user.FirstNameEn)},</p>
            <p>Click the link below to reset your Maydan password. This link expires in {ResetTokenValidityHours} hours and can only be used once.</p>
            <p><a href="{resetLink}">{resetLink}</a></p>
            <p>If you didn't request this, you can safely ignore this email.</p>
            """;

        await _emailSender.SendAsync(user.Email, "Reset your Maydan password", bodyHtml, cancellationToken);

        return Ok(new ForgotPasswordResponseDto(genericMessageEn), genericMessageAr, genericMessageEn);
    }

    public async Task<ApiResponse<ResetPasswordWithTokenResponseDto>> ResetPasswordWithTokenAsync(ResetPasswordWithTokenDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword) || string.IsNullOrWhiteSpace(dto.ConfirmPassword))
        {
            return Fail<ResetPasswordWithTokenResponseDto>("الرمز، كلمة المرور الجديدة، وتأكيدها مطلوبان.", "Token, new password, and confirmation are required.");
        }

        if (dto.NewPassword != dto.ConfirmPassword)
        {
            return Fail<ResetPasswordWithTokenResponseDto>("كلمة المرور الجديدة وتأكيدها غير متطابقتين.", "New password and confirmation do not match.");
        }

        var candidates = await _unitOfWork.PasswordResetTokens.GetAllAsync(cancellationToken);
        var matched = candidates.FirstOrDefault(t => _passwordHasher.VerifyPassword(dto.Token, t.TokenHash));

        if (matched is null)
        {
            return Fail<ResetPasswordWithTokenResponseDto>("رابط إعادة تعيين كلمة المرور غير صالح.", "This password reset link is invalid.");
        }

        if (matched.IsUsed)
        {
            return Fail<ResetPasswordWithTokenResponseDto>("تم استخدام رابط إعادة تعيين كلمة المرور هذا مسبقاً.", "This password reset link has already been used.");
        }

        if (matched.ExpiresAtUtc < DateTime.UtcNow)
        {
            return Fail<ResetPasswordWithTokenResponseDto>("انتهت صلاحية رابط إعادة تعيين كلمة المرور هذا.", "This password reset link has expired.");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(matched.UserId, cancellationToken);
        if (user is null)
        {
            return Fail<ResetPasswordWithTokenResponseDto>("رابط إعادة تعيين كلمة المرور غير صالح.", "This password reset link is invalid.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
        user.MustResetPassword = false;

        matched.IsUsed = true;
        matched.UsedAtUtc = DateTime.UtcNow;

        await RevokeAllActiveRefreshTokensAsync(user.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new ResetPasswordWithTokenResponseDto("Your password has been reset. You can now log in."), "تم إعادة تعيين كلمة المرور بنجاح. يمكنك تسجيل الدخول الآن.", "Your password has been reset. You can now log in.");
    }

    public async Task<ApiResponse<RefreshTokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
        {
            return Fail<RefreshTokenResponseDto>("رمز التحديث مطلوب.", "Refresh token is required.");
        }

        var candidates = await _unitOfWork.RefreshTokens.GetAllAsync(cancellationToken);
        var matched = candidates.FirstOrDefault(t => _passwordHasher.VerifyPassword(dto.RefreshToken, t.TokenHash));

        if (matched is null)
        {
            return Fail<RefreshTokenResponseDto>("رمز التحديث غير صالح.", "This refresh token is invalid.");
        }

        if (matched.IsRevoked)
        {
            if (matched.ReplacedByTokenId.HasValue)
            {
                await RevokeAllActiveRefreshTokensAsync(matched.UserId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Fail<RefreshTokenResponseDto>("تم استخدام رمز التحديث هذا مسبقاً.", "This refresh token has already been used.");
        }

        if (matched.ExpiresAtUtc < DateTime.UtcNow)
        {
            return Fail<RefreshTokenResponseDto>("انتهت صلاحية رمز التحديث هذا.", "This refresh token has expired.");
        }

        var user = await _unitOfWork.Users.GetWithPermissionsAsync(matched.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Fail<RefreshTokenResponseDto>("رمز التحديث غير صالح.", "This refresh token is invalid.");
        }

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(MapAuthUser(user));
        var generatedRefresh = _jwtTokenGenerator.GenerateRefreshToken();
        var newRefreshToken = new RefreshToken
        {
            UserId = matched.UserId,
            TokenHash = _passwordHasher.HashPassword(generatedRefresh.RawToken),
            ExpiresAtUtc = generatedRefresh.ExpiresAtUtc,
            IsRevoked = false
        };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _unitOfWork.RefreshTokens.AddAsync(newRefreshToken, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            matched.IsRevoked = true;
            matched.RevokedAtUtc = DateTime.UtcNow;
            matched.ReplacedByTokenId = newRefreshToken.Id;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return Ok(new RefreshTokenResponseDto(accessToken.AccessToken, accessToken.ExpiresAtUtc, generatedRefresh.RawToken, generatedRefresh.ExpiresAtUtc), "تم تحديث الرمز بنجاح.", "Token refreshed successfully.");
    }

    public async Task<ApiResponse<LogoutResponseDto>> LogoutAsync(LogoutRequestDto dto, CancellationToken cancellationToken = default)
    {
        const string messageAr = "تم تسجيل الخروج بنجاح.";
        const string messageEn = "Logged out.";

        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
        {
            return Ok(new LogoutResponseDto(messageEn), messageAr, messageEn);
        }

        var candidates = await _unitOfWork.RefreshTokens.GetAllAsync(cancellationToken);
        var matched = candidates.FirstOrDefault(t => _passwordHasher.VerifyPassword(dto.RefreshToken, t.TokenHash));

        if (matched is not null && !matched.IsRevoked)
        {
            matched.IsRevoked = true;
            matched.RevokedAtUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Ok(new LogoutResponseDto(messageEn), messageAr, messageEn);
    }

    private async Task<(string RawToken, DateTime ExpiresAtUtc)> IssueRefreshTokenAsync(int userId, CancellationToken cancellationToken)
    {
        var generated = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = _passwordHasher.HashPassword(generated.RawToken),
            ExpiresAtUtc = generated.ExpiresAtUtc,
            IsRevoked = false
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (generated.RawToken, generated.ExpiresAtUtc);
    }

    private async Task RevokeAllActiveRefreshTokensAsync(int userId, CancellationToken cancellationToken)
    {
        var tokens = await _unitOfWork.RefreshTokens.GetAllAsync(cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var token in tokens.Where(t => t.UserId == userId && !t.IsRevoked))
        {
            token.IsRevoked = true;
            token.RevokedAtUtc = now;
        }
    }

    private static string GenerateRawResetToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static AuthUserDto MapAuthUser(User user)
    {
        var permissionIds = new HashSet<int>();

        if (user.Role?.RolePermissions != null)
        {
            foreach (var rp in user.Role.RolePermissions)
            {
                permissionIds.Add(rp.PermissionId);
            }
        }

        if (user.UserPermissions != null)
        {
            foreach (var up in user.UserPermissions)
            {
                permissionIds.Add(up.PermissionId);
            }
        }

        if (user.UserGroups != null)
        {
            foreach (var ug in user.UserGroups)
            {
                if (ug.Group?.GroupPermissions != null)
                {
                    foreach (var gp in ug.Group.GroupPermissions)
                    {
                        permissionIds.Add(gp.PermissionId);
                    }
                }
            }
        }

        return new AuthUserDto(
            user.UserId,
            user.Email,
            user.FirstNameEn,
            user.LastNameEn,
            user.FirstNameAr,
            user.LastNameAr,
            user.RoleId,
            user.Role?.RoleNameEn ?? string.Empty,
            user.Role?.RoleNameAr ?? string.Empty,
            user.EntityType,
            user.EntityId,
            permissionIds.OrderBy(id => id).ToList() 
        );
    }

    #region Helpers

    private static ApiResponse<T> Fail<T>(string messageAr, string messageEn) =>
        new(false, messageAr, messageEn, default, 400);

    private static ApiResponse<T> Ok<T>(T data, string messageAr, string messageEn, int statusCode = 200) =>
        new(true, messageAr, messageEn, data, statusCode);

    #endregion
}