using Maydan.Domain.Enums;

namespace Maydan.Application.DTOs.Auth;

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    // Real 3-week session persistence / "remember me" (MAYD-131/132, 2026-09-23). Defaults to false
    // (bool's own default) so any caller that doesn't send this field at all keeps today's exact
    // behavior — access-token-only, no refresh token issued. See AuthService.LoginAsync's own
    // comment for why a refresh token being issued at all IS the "remember me" mechanism.
    public bool RememberMe { get; set; }
}

public record ResetPasswordDto(string Email, string CurrentPassword, string NewPassword);

// Forgot-password recovery (MAYD-128/129/130, 2026-09-23): deliberately separate from
// ResetPasswordDto above — that flow requires knowing the CURRENT password (forced-first-login /
// the existing "تغيير كلمة المرور" link); this one is the genuine "I don't know my password at
// all" recovery path, gated by proving control of the account's email instead.
public record ForgotPasswordDto(string Email);

// Always the same message whether or not the email is registered — see
// AuthService.ForgotPasswordAsync's own comment on why (email-enumeration safety, this story's own
// requirement).
public record ForgotPasswordResponseDto(string Message);

public record ResetPasswordWithTokenDto(string Token, string NewPassword, string ConfirmPassword);

public record ResetPasswordWithTokenResponseDto(string Message);

// Real 3-week session persistence / "remember me" (MAYD-131/132, 2026-09-23). Every field here is
// non-nullable/required — unlike the password-reset DTOs, there is no "may or may not have
// happened" outcome for a successful refresh: it either throws (invalid/expired/already-used token)
// or returns a genuinely fresh access+refresh token pair, always both together (rotation).
public record RefreshTokenRequestDto(string RefreshToken);

public record RefreshTokenResponseDto(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

// Deliberately always the same generic success message regardless of whether the presented token
// was real, already revoked, or missing entirely — see AuthService.LogoutAsync's own comment on why
// that keeps this endpoint simple and idempotent rather than a security requirement like
// ForgotPasswordAsync's enumeration-safety.
public record LogoutRequestDto(string RefreshToken);

public record LogoutResponseDto(string Message);

public record LoginResponseDto(
    bool IsAuthenticated,
    bool MustResetPassword,
    string? AccessToken,
    DateTime? AccessTokenExpiresAtUtc,
    AuthUserDto? User,
    string? Message,
    // Real 3-week session persistence / "remember me" (MAYD-131/132, 2026-09-23). Non-null ONLY when
    // LoginRequestDto.RememberMe was true (see AuthService.LoginAsync) — every other caller of this
    // record (the mustResetPassword branch, ResetPasswordAsync's own success response) leaves these
    // at their default null, identical to today's shape.
    string? RefreshToken = null,
    DateTime? RefreshTokenExpiresAtUtc = null);

public record AuthUserDto(
   int UserId,
    string Email,
    string FirstNameEn,
    string LastNameEn,
    string FirstNameAr,
    string LastNameAr,
    int RoleId,
    string RoleNameEn,
    string RoleNameAr,
    EntityType EntityType,
    int EntityId,
    List<int> Permissions);

// Entity onboarding Stage 1 (2026-09-22): public production-company self-registration — company +
// its first admin in one submission, active immediately (confirmed product decision). Matches
// production-house-signup.component.ts's form fields field-for-field.
// Stage 3 (2026-09-22): AdminFirstNameAr/AdminLastNameAr added now that the form collects the
// admin's real Arabic name — naming mirrors AdminFirstName/AdminLastName's own "Admin" prefix,
// plus the "NameAr" suffix Stage 2's OnboardAssociationAdminDto already established.
public record RegisterProductionCompanyDto(
    string CompanyNameEn,
    string CompanyNameAr,
    string RegistrationNumber,
    int CityId,
    string AdminFirstName,
    string AdminLastName,
    string AdminFirstNameAr,
    string AdminLastNameAr,
    string MobileCountryCode,
    string MobileNumber,
    string Email,
    string Password);

public record RegisterProductionCompanyResponseDto(
    int ProductionCompanyId,
    string CompanyNameEn,
    string CompanyNameAr,
    int AdminUserId,
    string AdminEmail,
    string Message);
