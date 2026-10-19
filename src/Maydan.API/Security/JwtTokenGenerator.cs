using Maydan.Application.DTOs.Auth;
using Maydan.Application.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Maydan.API.Security;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string AccessToken, DateTime ExpiresAtUtc) GenerateAccessToken(AuthUserDto user)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var accessTokenMinutes = int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out var configuredMinutes)
            ? configuredMinutes
            : 15;

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(accessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Email, user.Email),
            new("roleId", user.RoleId.ToString()),
            new(ClaimTypes.Role, user.RoleNameEn),
            new("roleNameEn", user.RoleNameEn),
            new("roleNameAr", user.RoleNameAr),
            new("entityType", user.EntityType.ToString()),
            new("entityId", user.EntityId.ToString())
        };

        claims.AddRange(user.Permissions.Select(permissionIds => new Claim("permission", permissionIds.ToString())));

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    // Sliding 3-week window (Business Rule #7, interpreted as sliding — see AuthService.cs's own
    // comment): every call (login with rememberMe, or a successful rotation) returns a fresh expiry
    // this many days out from now, not from some fixed original-login timestamp. Jwt:RefreshTokenDays
    // previously existed in appsettings.json but nothing read it — genuinely wired up now; falls back
    // to 21 (3 weeks) if missing/unparsable, same defensive pattern AccessTokenMinutes above uses.
    public (string RawToken, DateTime ExpiresAtUtc) GenerateRefreshToken()
    {
        var refreshTokenDays = int.TryParse(_configuration["Jwt:RefreshTokenDays"], out var configuredDays)
            ? configuredDays
            : 21;

        var expiresAtUtc = DateTime.UtcNow.AddDays(refreshTokenDays);

        // 256 bits of entropy, base64url-encoded — same shape/rationale as
        // AuthService.GenerateRawResetToken (safe to place directly in a URL/JSON body with no extra
        // escaping), duplicated rather than shared since the two live in different layers
        // (Application vs. this API-layer class) and are otherwise unrelated concerns.
        var bytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        return (rawToken, expiresAtUtc);
    }
}
