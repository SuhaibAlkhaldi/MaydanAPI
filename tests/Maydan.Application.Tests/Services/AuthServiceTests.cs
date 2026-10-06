using Maydan.Application.DTOs.Auth;
using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;
using Maydan.Infrastructure.Security;

namespace Maydan.Application.Tests.Services;

// Forgot-password recovery (MAYD-128/129/130) and real 3-week session persistence / "remember me"
// (MAYD-131/132, 2026-09-23): uses the REAL PasswordHasher (not a fake) — the whole point of these
// tests is proving the actual salted-hash generate/verify round trip works for reset AND refresh
// tokens the same way it already does for passwords (see PasswordResetToken.cs/RefreshToken.cs's
// own comments on why a token lookup can't be a direct hash-equality query).
public class AuthServiceTests
{
    private static Role AssociationRole() => new() { RoleId = 4, RoleNameEn = "Association", RoleNameAr = "الجمعية" };

    private static User ActiveUser(int userId = 1, string email = "user@example.org", bool mustResetPassword = false) => new()
    {
        UserId = userId,
        Email = email,
        FirstNameEn = "Test",
        LastNameEn = "User",
        FirstNameAr = "اختبار",
        LastNameAr = "مستخدم",
        PhoneNumber = "+962700000000",
        PasswordHash = new PasswordHasher().HashPassword("OldP@ssw0rd!"),
        MustResetPassword = mustResetPassword,
        IsActive = true,
        RoleId = 4,
        Role = AssociationRole(),
        EntityType = EntityType.Association,
        EntityId = 1
    };

    private static (AuthService service, FakeUserRepository users, FakePasswordResetTokenRepository resetTokens, FakeRefreshTokenRepository refreshTokens, FakeEmailSender emailSender, FakeFrontendLinkBuilder linkBuilder)
        CreateService(User? existingUser = null)
    {
        var users = new FakeUserRepository(existingUser);
        var resetTokens = new FakePasswordResetTokenRepository();
        var refreshTokens = new FakeRefreshTokenRepository();
        var emailSender = new FakeEmailSender();
        var linkBuilder = new FakeFrontendLinkBuilder();
        var unitOfWork = new FakeUnitOfWork(users, resetTokens, refreshTokens);
        var service = new AuthService(unitOfWork, new PasswordHasher(), new FakeJwtTokenGenerator(), emailSender, linkBuilder);

        return (service, users, resetTokens, refreshTokens, emailSender, linkBuilder);
    }

    private static string ExtractToken(string link) => Uri.UnescapeDataString(link.Split("token=")[1]);

    public class ForgotPasswordAsyncTests
    {
        [Fact]
        public async Task ExistingActiveUser_CreatesAHashedTokenAndEmailsARealResetLink()
        {
            var user = ActiveUser();
            var (service, users, resetTokens, _, emailSender, _) = CreateService(user);

            var result = await service.ForgotPasswordAsync(new ForgotPasswordDto(user.Email));

            Assert.NotNull(resetTokens.AddedToken);
            Assert.Equal(user.UserId, resetTokens.AddedToken!.UserId);
            Assert.False(resetTokens.AddedToken.IsUsed);
            // Not the raw token — a salted hash of it (PasswordHasher's own format), same as passwords.
            Assert.DoesNotContain(ExtractToken(emailSender.LastLink!), resetTokens.AddedToken.TokenHash);

            Assert.Equal(1, emailSender.SendCallCount);
            Assert.Equal(user.Email, emailSender.LastToEmail);
            Assert.NotNull(result.Data!.Message);
            _ = users;
        }

        [Fact]
        public async Task TokenGenerated_HashVerifiesAgainstTheRawTokenEmailedToTheUser()
        {
            var user = ActiveUser();
            var (service, _, resetTokens, _, emailSender, _) = CreateService(user);

            await service.ForgotPasswordAsync(new ForgotPasswordDto(user.Email));

            var rawToken = ExtractToken(emailSender.LastLink!);
            var hasher = new PasswordHasher();
            Assert.True(hasher.VerifyPassword(rawToken, resetTokens.AddedToken!.TokenHash));
        }

        [Fact]
        public async Task SetsExpiryTo24HoursFromNow()
        {
            var user = ActiveUser();
            var (service, _, resetTokens, _, _, _) = CreateService(user);
            var before = DateTime.UtcNow;

            await service.ForgotPasswordAsync(new ForgotPasswordDto(user.Email));

            var after = DateTime.UtcNow;
            Assert.InRange(resetTokens.AddedToken!.ExpiresAtUtc, before.AddHours(24).AddSeconds(-5), after.AddHours(24).AddSeconds(5));
        }

        [Fact]
        public async Task UnknownEmail_ReturnsTheSameGenericMessageAndSendsNoEmail_EnumerationSafety()
        {
            var (existingService, _, _, _, existingEmailSender, _) = CreateService(ActiveUser(email: "real@example.org"));
            var existingResult = await existingService.ForgotPasswordAsync(new ForgotPasswordDto("real@example.org"));

            var (unknownService, _, unknownResetTokens, _, unknownEmailSender, _) = CreateService(existingUser: null);
            var unknownResult = await unknownService.ForgotPasswordAsync(new ForgotPasswordDto("nobody@example.org"));

            Assert.Equal(existingResult.Data!.Message, unknownResult.Data!.Message);
            Assert.Equal(0, unknownEmailSender.SendCallCount);
            Assert.Null(unknownResetTokens.AddedToken);
        }

        [Fact]
        public async Task InactiveUser_TreatedTheSameAsUnknownEmail_EnumerationSafety()
        {
            var inactiveUser = ActiveUser();
            inactiveUser.IsActive = false;
            var (service, _, resetTokens, _, emailSender, _) = CreateService(inactiveUser);

            var result = await service.ForgotPasswordAsync(new ForgotPasswordDto(inactiveUser.Email));

            Assert.Equal(0, emailSender.SendCallCount);
            Assert.Null(resetTokens.AddedToken);
            Assert.Contains("if an account", result.Data!.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task BlankEmail_ThrowsARealValidationError_NotTheEnumerationSafeMessage()
        {
            var (service, _, _, _, _, _) = CreateService();

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ForgotPasswordAsync(new ForgotPasswordDto("")));
        }

        // Decision confirmed 2026-09-23: requesting a new link invalidates any earlier unused,
        // unexpired token for that same user (reusing IsUsed rather than adding a separate
        // "superseded" reason — ResetPasswordWithTokenAsync's existing IsUsed check already rejects
        // it with the same real "already used" message, no new error path needed). Otherwise an old,
        // forgotten link sitting in an inbox or browser history would stay usable right alongside a
        // newer one until it separately expired.
        [Fact]
        public async Task SecondRequest_InvalidatesTheFirstStillOutstandingToken_OldOneRejectedNewOneStillWorks()
        {
            var user = ActiveUser();
            var (service, _, resetTokens, _, emailSender, _) = CreateService(user);

            await service.ForgotPasswordAsync(new ForgotPasswordDto(user.Email));
            var firstRawToken = ExtractToken(emailSender.SentLinks[0]);

            await service.ForgotPasswordAsync(new ForgotPasswordDto(user.Email));
            var secondRawToken = ExtractToken(emailSender.SentLinks[1]);

            Assert.Equal(2, resetTokens.AllTokens.Count);
            Assert.True(resetTokens.AllTokens[0].IsUsed); // superseded by the second request
            Assert.False(resetTokens.AllTokens[1].IsUsed); // the one actually just issued

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ResetPasswordWithTokenAsync(new ResetPasswordWithTokenDto(firstRawToken, "NewP@ssw0rd!", "NewP@ssw0rd!")));
            Assert.Contains("already", exception.Message, StringComparison.OrdinalIgnoreCase);

            var result = await service.ResetPasswordWithTokenAsync(new ResetPasswordWithTokenDto(secondRawToken, "NewP@ssw0rd!", "NewP@ssw0rd!"));
            Assert.Contains("reset", result.Data!.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class ResetPasswordWithTokenAsyncTests
    {
        private static async Task<(AuthService service, FakeUserRepository users, FakePasswordResetTokenRepository resetTokens, FakeRefreshTokenRepository refreshTokens, string rawToken, User user)>
            CreateServiceWithAnIssuedToken(bool mustResetPassword = false)
        {
            var user = ActiveUser(mustResetPassword: mustResetPassword);
            var (service, users, resetTokens, refreshTokens, emailSender, _) = CreateService(user);
            await service.ForgotPasswordAsync(new ForgotPasswordDto(user.Email));
            var rawToken = ExtractToken(emailSender.LastLink!);

            return (service, users, resetTokens, refreshTokens, rawToken, user);
        }

        [Fact]
        public async Task ValidToken_ResetsThePasswordAndMarksTheTokenUsed()
        {
            var (service, users, resetTokens, _, rawToken, user) = await CreateServiceWithAnIssuedToken();

            var result = await service.ResetPasswordWithTokenAsync(new ResetPasswordWithTokenDto(rawToken, "NewP@ssw0rd!", "NewP@ssw0rd!"));

            Assert.True(resetTokens.AddedToken!.IsUsed);
            Assert.NotNull(resetTokens.AddedToken.UsedAtUtc);
            Assert.True(new PasswordHasher().VerifyPassword("NewP@ssw0rd!", users.TrackedUser!.PasswordHash));
            Assert.False(users.TrackedUser.MustResetPassword);
            Assert.Contains("reset", result.Data!.Message, StringComparison.OrdinalIgnoreCase);
            _ = user;
        }

        [Fact]
        public async Task ValidToken_AlsoClearsMustResetPassword_SoTheUserIsNotForcedThroughThatFlowRightAfter()
        {
            var (service, users, _, _, rawToken, _) = await CreateServiceWithAnIssuedToken(mustResetPassword: true);

            await service.ResetPasswordWithTokenAsync(new ResetPasswordWithTokenDto(rawToken, "NewP@ssw0rd!", "NewP@ssw0rd!"));

            Assert.False(users.TrackedUser!.MustResetPassword);
        }

        [Fact]
        public async Task OldPasswordNoLongerVerifiesAfterAReset_TheStorysOwnFirstAcceptanceCriterion()
        {
            var (service, users, _, _, rawToken, _) = await CreateServiceWithAnIssuedToken();

            await service.ResetPasswordWithTokenAsync(new ResetPasswordWithTokenDto(rawToken, "NewP@ssw0rd!", "NewP@ssw0rd!"));

            Assert.False(new PasswordHasher().VerifyPassword("OldP@ssw0rd!", users.TrackedUser!.PasswordHash));
        }

        [Fact]
        public async Task UsedToken_RejectedOnASecondAttempt_WithADistinctAlreadyUsedMessage()
        {
            var (service, _, _, _, rawToken, _) = await CreateServiceWithAnIssuedToken();
            await service.ResetPasswordWithTokenAsync(new ResetPasswordWithTokenDto(rawToken, "NewP@ssw0rd!", "NewP@ssw0rd!"));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ResetPasswordWithTokenAsync(new ResetPasswordWithTokenDto(rawToken, "AnotherP@ss1!", "AnotherP@ss1!")));

            Assert.Contains("already", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ExpiredToken_RejectedWithADistinctExpiredMessage()
        {
            var (service, _, resetTokens, _, rawToken, _) = await CreateServiceWithAnIssuedToken();
            resetTokens.AddedToken!.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ResetPasswordWithTokenAsync(new ResetPasswordWithTokenDto(rawToken, "NewP@ssw0rd!", "NewP@ssw0rd!")));

            Assert.Contains("expired", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UnknownToken_RejectedWithADistinctInvalidMessage()
        {
            var (service, _, _, _, _, _) = await CreateServiceWithAnIssuedToken();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ResetPasswordWithTokenAsync(new ResetPasswordWithTokenDto("not-a-real-token", "NewP@ssw0rd!", "NewP@ssw0rd!")));

            Assert.Contains("invalid", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task MismatchedConfirmation_Rejected()
        {
            var (service, _, _, _, rawToken, _) = await CreateServiceWithAnIssuedToken();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ResetPasswordWithTokenAsync(new ResetPasswordWithTokenDto(rawToken, "NewP@ssw0rd!", "Different1!")));

            Assert.Contains("match", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        // Security hardening (MAYD-131/132, 2026-09-23).
        [Fact]
        public async Task ValidToken_RevokesEveryActiveRefreshTokenForThatUser()
        {
            var (service, users, _, refreshTokens, rawToken, user) = await CreateServiceWithAnIssuedToken();
            var survivingSession = refreshTokens.AddActiveToken(user.UserId);
            var otherUsersSession = refreshTokens.AddActiveToken(userId: 999);

            await service.ResetPasswordWithTokenAsync(new ResetPasswordWithTokenDto(rawToken, "NewP@ssw0rd!", "NewP@ssw0rd!"));

            Assert.True(refreshTokens.AllTokens.Single(t => t.Id == survivingSession.Id).IsRevoked);
            // Only THIS user's tokens — a different user's active session is left alone.
            Assert.False(refreshTokens.AllTokens.Single(t => t.Id == otherUsersSession.Id).IsRevoked);
            _ = users;
        }
    }

    // Real 3-week session persistence / "remember me" (MAYD-131/132, 2026-09-23).
    public class LoginAsyncTests
    {
        [Fact]
        public async Task RememberMeTrue_IssuesARealHashedRefreshTokenAlongsideTheAccessToken()
        {
            var user = ActiveUser();
            var (service, _, _, refreshTokens, _, _) = CreateService(user);

            var result = await service.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "OldP@ssw0rd!", RememberMe = true });

            Assert.NotNull(result.Data!.RefreshToken);
            Assert.NotNull(result.Data!.RefreshTokenExpiresAtUtc);
            Assert.NotNull(refreshTokens.AddedToken);
            Assert.Equal(user.UserId, refreshTokens.AddedToken!.UserId);
            Assert.False(refreshTokens.AddedToken.IsRevoked);
            // Not the raw token — a salted hash of it, same convention as password-reset tokens.
            Assert.DoesNotContain(result.Data!.RefreshToken!, refreshTokens.AddedToken.TokenHash);
            Assert.True(new PasswordHasher().VerifyPassword(result.Data!.RefreshToken!, refreshTokens.AddedToken.TokenHash));
        }

        [Fact]
        public async Task RememberMeTrue_SetsASlidingExpiryRoughly21DaysOut()
        {
            var user = ActiveUser();
            var (service, _, _, _, _, _) = CreateService(user);
            var before = DateTime.UtcNow;

            var result = await service.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "OldP@ssw0rd!", RememberMe = true });

            var after = DateTime.UtcNow;
            Assert.InRange(result.Data!.RefreshTokenExpiresAtUtc!.Value, before.AddDays(21).AddSeconds(-5), after.AddDays(21).AddSeconds(5));
        }

        [Fact]
        public async Task RememberMeFalseOrOmitted_IssuesNoRefreshTokenAtAll()
        {
            var user = ActiveUser();
            var (service, _, _, refreshTokens, _, _) = CreateService(user);

            var result = await service.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "OldP@ssw0rd!" });

            Assert.Null(result.Data!.RefreshToken);
            Assert.Null(result.Data!.RefreshTokenExpiresAtUtc);
            Assert.Null(refreshTokens.AddedToken);
        }

        [Fact]
        public async Task MustResetPasswordBranch_IssuesNoRefreshTokenEvenWithRememberMeTrue()
        {
            var user = ActiveUser(mustResetPassword: true);
            var (service, _, _, refreshTokens, _, _) = CreateService(user);

            var result = await service.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "OldP@ssw0rd!", RememberMe = true });

            Assert.True(result.Data!.MustResetPassword);
            Assert.Null(result.Data!.RefreshToken);
            Assert.Null(refreshTokens.AddedToken);
        }
    }

    // Real 3-week session persistence (MAYD-131/132, 2026-09-23): rotation, reuse detection,
    // expiry and revocation-rejection — not just the happy path.
    public class RefreshTokenAsyncTests
    {
        private static async Task<(AuthService service, FakeUserRepository users, FakeRefreshTokenRepository refreshTokens, string rawToken, User user)>
            CreateServiceWithAnIssuedRefreshToken()
        {
            var user = ActiveUser();
            var (service, users, _, refreshTokens, _, _) = CreateService(user);
            var login = await service.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "OldP@ssw0rd!", RememberMe = true });

            return (service, users, refreshTokens, login.Data!.RefreshToken!, user);
        }

        [Fact]
        public async Task ValidToken_RotatesToANewAccessAndRefreshTokenPair()
        {
            var (service, _, refreshTokens, rawToken, _) = await CreateServiceWithAnIssuedRefreshToken();

            var result = await service.RefreshTokenAsync(new RefreshTokenRequestDto(rawToken));

            Assert.NotNull(result.Data!.AccessToken);
            Assert.NotNull(result.Data!.RefreshToken);
            Assert.NotEqual(rawToken, result.Data!.RefreshToken);
            Assert.Equal(2, refreshTokens.AllTokens.Count);
        }

        [Fact]
        public async Task ValidToken_RevokesTheOldTokenAndPointsItAtItsReplacement()
        {
            var (service, _, refreshTokens, rawToken, _) = await CreateServiceWithAnIssuedRefreshToken();
            var oldTokenId = refreshTokens.AddedToken!.Id;

            await service.RefreshTokenAsync(new RefreshTokenRequestDto(rawToken));

            var oldToken = refreshTokens.AllTokens.Single(t => t.Id == oldTokenId);
            var newToken = refreshTokens.AllTokens.Single(t => t.Id != oldTokenId);
            Assert.True(oldToken.IsRevoked);
            Assert.Equal(newToken.Id, oldToken.ReplacedByTokenId);
            Assert.False(newToken.IsRevoked);
        }

        [Fact]
        public async Task OldTokenIsRejectedAfterRotation()
        {
            var (service, _, _, rawToken, _) = await CreateServiceWithAnIssuedRefreshToken();
            await service.RefreshTokenAsync(new RefreshTokenRequestDto(rawToken));

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.RefreshTokenAsync(new RefreshTokenRequestDto(rawToken)));

            Assert.Contains("already", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task NewTokenStillWorksAfterTheOldOneWasRotatedAway()
        {
            var (service, _, _, rawToken, _) = await CreateServiceWithAnIssuedRefreshToken();
            var rotated = await service.RefreshTokenAsync(new RefreshTokenRequestDto(rawToken));

            var second = await service.RefreshTokenAsync(new RefreshTokenRequestDto(rotated.Data!.RefreshToken));

            Assert.NotNull(second.Data!.AccessToken);
            Assert.NotNull(second.Data!.RefreshToken);
        }

        // The real theft-detection scenario: someone replays a raw token value that has ALREADY been
        // exchanged for a newer one (e.g. an attacker who captured it earlier, or a client retrying a
        // stale value after its own successful rotation). Every other active session for that user is
        // killed in response, not just this one token.
        [Fact]
        public async Task ReusingAnAlreadyRotatedToken_RevokesEveryOtherActiveTokenForThatUser()
        {
            var (service, _, refreshTokens, rawToken, user) = await CreateServiceWithAnIssuedRefreshToken();
            var rotated = await service.RefreshTokenAsync(new RefreshTokenRequestDto(rawToken));
            // A second legitimate-looking session for the same user, unrelated to the rotation chain.
            var unrelatedSession = refreshTokens.AddActiveToken(user.UserId);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.RefreshTokenAsync(new RefreshTokenRequestDto(rawToken)));

            Assert.True(refreshTokens.AllTokens.Single(t => t.Id == unrelatedSession.Id).IsRevoked);
            // The token that replaced the reused one is ALSO revoked by the chain-kill — a stolen
            // token's rotation chain is fully dead, not just the specific value that got reused.
            var hasher = new PasswordHasher();
            Assert.True(refreshTokens.AllTokens.Single(t => hasher.VerifyPassword(rotated.Data!.RefreshToken, t.TokenHash)).IsRevoked);
        }

        [Fact]
        public async Task ExpiredToken_Rejected()
        {
            var (service, _, refreshTokens, rawToken, _) = await CreateServiceWithAnIssuedRefreshToken();
            refreshTokens.AddedToken!.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1);

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.RefreshTokenAsync(new RefreshTokenRequestDto(rawToken)));

            Assert.Contains("expired", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UnknownToken_Rejected()
        {
            var (service, _, _, _, _) = await CreateServiceWithAnIssuedRefreshToken();

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.RefreshTokenAsync(new RefreshTokenRequestDto("not-a-real-refresh-token")));

            Assert.Contains("invalid", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task BlankToken_ThrowsARealValidationError_DistinctFromAnInvalidToken()
        {
            var (service, _, _, _, _, _) = CreateService();

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RefreshTokenAsync(new RefreshTokenRequestDto("")));
        }

        [Fact]
        public async Task RevokedViaLogout_RejectedOnRefresh()
        {
            var (service, _, refreshTokens, rawToken, _) = await CreateServiceWithAnIssuedRefreshToken();
            await service.LogoutAsync(new LogoutRequestDto(rawToken));

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.RefreshTokenAsync(new RefreshTokenRequestDto(rawToken)));

            Assert.Contains("already", exception.Message, StringComparison.OrdinalIgnoreCase);
            // Revoked directly (logout), not via rotation — no chain-kill side effect, only this one
            // token is affected. (No other tokens exist in this test to assert on, but the important
            // thing already proven above is that a plain logout revocation alone still rejects reuse.)
            Assert.Null(refreshTokens.AddedToken!.ReplacedByTokenId);
        }
    }

    // Real 3-week session persistence (MAYD-131/132, 2026-09-23).
    public class LogoutAsyncTests
    {
        [Fact]
        public async Task ValidToken_IsRevoked()
        {
            var user = ActiveUser();
            var (service, _, _, refreshTokens, _, _) = CreateService(user);
            var login = await service.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "OldP@ssw0rd!", RememberMe = true });

            await service.LogoutAsync(new LogoutRequestDto(login.Data!.RefreshToken!));

            Assert.True(refreshTokens.AddedToken!.IsRevoked);
            Assert.NotNull(refreshTokens.AddedToken.RevokedAtUtc);
        }

        [Fact]
        public async Task RevokedToken_IsThenRejectedByRefresh()
        {
            var user = ActiveUser();
            var (service, _, _, _, _, _) = CreateService(user);
            var login = await service.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "OldP@ssw0rd!", RememberMe = true });
            await service.LogoutAsync(new LogoutRequestDto(login.Data!.RefreshToken!));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.RefreshTokenAsync(new RefreshTokenRequestDto(login.Data!.RefreshToken!)));
        }

        // Deliberately never throws — see AuthService.LogoutAsync's own comment on why this stays a
        // simple, idempotent no-op rather than a security-relevant rejection.
        [Fact]
        public async Task UnknownOrMissingToken_StillReturnsTheGenericSuccessMessage()
        {
            var (service, _, _, _, _, _) = CreateService();

            var blank = await service.LogoutAsync(new LogoutRequestDto(""));
            var unknown = await service.LogoutAsync(new LogoutRequestDto("not-a-real-token"));

            Assert.NotNull(blank.Data!.Message);
            Assert.Equal(blank.Data!.Message, unknown.Data!.Message);
        }
    }

    // Security hardening (MAYD-131/132, 2026-09-23) — the forced-reset path (ResetPasswordAsync,
    // AuthController's own `reset-password` route) had NO tests at all before this stage; covering
    // its core behavior here alongside the new revoke-all-refresh-tokens requirement.
    public class ResetPasswordAsyncTests
    {
        [Fact]
        public async Task ValidCurrentPassword_ChangesThePasswordAndClearsMustResetPassword()
        {
            var user = ActiveUser(mustResetPassword: true);
            var (service, users, _, _, _, _) = CreateService(user);

            var result = await service.ResetPasswordAsync(new ResetPasswordDto(user.Email, "OldP@ssw0rd!", "NewP@ssw0rd!"));

            Assert.True(new PasswordHasher().VerifyPassword("NewP@ssw0rd!", users.TrackedUser!.PasswordHash));
            Assert.False(users.TrackedUser.MustResetPassword);
            Assert.NotNull(result.Data!.AccessToken);
        }

        [Fact]
        public async Task WrongCurrentPassword_Rejected()
        {
            var user = ActiveUser(mustResetPassword: true);
            var (service, _, _, _, _, _) = CreateService(user);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.ResetPasswordAsync(new ResetPasswordDto(user.Email, "WrongPassword!", "NewP@ssw0rd!")));
        }

        // Login/register cleanup follow-up (2026-09-23): the MustResetPassword gate this test used to
        // cover (rejecting ANY caller whose account wasn't already flagged for a forced reset) was
        // removed from AuthService.ResetPasswordAsync itself — real bug found via a live browser test
        // of the new (authenticated) ChangePasswordComponent, which reuses this exact endpoint and
        // was getting rejected even with the correct current password. This test now confirms the
        // opposite of what it used to: a normal, already-fine account (the real case
        // ChangePasswordComponent hits) CAN voluntarily change its password here, same as the forced-
        // reset case above — the current-password check is the only real gate this endpoint needs.
        [Fact]
        public async Task NormalAccount_MustResetPasswordAlreadyFalse_CanStillVoluntarilyChangePassword()
        {
            var user = ActiveUser(mustResetPassword: false);
            var (service, users, _, _, _, _) = CreateService(user);

            var result = await service.ResetPasswordAsync(new ResetPasswordDto(user.Email, "OldP@ssw0rd!", "NewP@ssw0rd!"));

            Assert.True(new PasswordHasher().VerifyPassword("NewP@ssw0rd!", users.TrackedUser!.PasswordHash));
            Assert.False(users.TrackedUser.MustResetPassword);
            Assert.NotNull(result.Data!.AccessToken);
        }

        // Security hardening (MAYD-131/132, 2026-09-23): a password change through THIS path (the
        // forced-first-login flow, and the existing "تغيير كلمة المرور" logged-out change-password
        // flow — both real UI flows hit this exact same backend endpoint) kills every other active
        // session too.
        [Fact]
        public async Task PasswordChange_RevokesEveryActiveRefreshTokenForThatUser()
        {
            var user = ActiveUser(mustResetPassword: true);
            var (service, _, _, refreshTokens, _, _) = CreateService(user);
            var survivingSession = refreshTokens.AddActiveToken(user.UserId);
            var otherUsersSession = refreshTokens.AddActiveToken(userId: 999);

            await service.ResetPasswordAsync(new ResetPasswordDto(user.Email, "OldP@ssw0rd!", "NewP@ssw0rd!"));

            Assert.True(refreshTokens.AllTokens.Single(t => t.Id == survivingSession.Id).IsRevoked);
            Assert.False(refreshTokens.AllTokens.Single(t => t.Id == otherUsersSession.Id).IsRevoked);
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User? _user;

        public FakeUserRepository(User? user) => _user = user;

        public User? TrackedUser { get; private set; }

        public Task<User?> GetByEmailWithAccessAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_user is not null && string.Equals(_user.Email, email, StringComparison.OrdinalIgnoreCase) ? _user : null);

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            if (_user is null || _user.UserId != userId)
            {
                return Task.FromResult<User?>(null);
            }

            TrackedUser = _user;
            return Task.FromResult<User?>(_user);
        }

        // RefreshTokenAsync's own use (needs the full Role/Permissions graph for MapAuthUser, the
        // same reason GetByEmailWithAccessAsync already loads it for LoginAsync) — reuses the same
        // single seeded user, matched by Id instead of email.
        public Task<User?> GetWithPermissionsAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_user is not null && _user.UserId == userId ? _user : null);

        public Task<List<User>> GetByEntityAsync(EntityType entityType, int entityId, string? search, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetDeletedByEntityAsync(EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(List<User> Users, int TotalCount)> GetPagedByEntityAsync(EntityType entityType, int entityId, string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetDetailsReadOnlyAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameEnAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUserNameArAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<User>> GetByIdsInEntityAsync(IEnumerable<int> userIds, EntityType entityType, int entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(User user) => throw new NotSupportedException();
    }

    // Backs GetAllAsync with every token ever added (not just the latest) — needed to exercise
    // ForgotPasswordAsync's own invalidate-outstanding-tokens step, which reads the full list back
    // via the same GetAllAsync the real repository uses. AddedToken stays as an alias for the most
    // recently added token, since every pre-existing test only ever adds one.
    private sealed class FakePasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly List<PasswordResetToken> _tokens = new();

        public IReadOnlyList<PasswordResetToken> AllTokens => _tokens;
        public PasswordResetToken? AddedToken => _tokens.Count == 0 ? null : _tokens[^1];

        public Task<List<PasswordResetToken>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<PasswordResetToken>(_tokens));

        public Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
        {
            token.Id = _tokens.Count + 1;
            _tokens.Add(token);
            return Task.CompletedTask;
        }
    }

    // Same shape as FakePasswordResetTokenRepository above. AddActiveToken() lets a test seed an
    // extra, unrelated (or same-user) still-valid session directly — used to prove the
    // revoke-all-active-tokens behavior only ever touches the right user's rows.
    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly List<RefreshToken> _tokens = new();

        public IReadOnlyList<RefreshToken> AllTokens => _tokens;
        public RefreshToken? AddedToken => _tokens.Count == 0 ? null : _tokens[^1];

        public Task<List<RefreshToken>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<RefreshToken>(_tokens));

        public Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
        {
            token.Id = _tokens.Count + 1;
            _tokens.Add(token);
            return Task.CompletedTask;
        }

        public RefreshToken AddActiveToken(int userId)
        {
            var token = new RefreshToken
            {
                Id = _tokens.Count + 1,
                UserId = userId,
                TokenHash = new PasswordHasher().HashPassword($"seed-token-{Guid.NewGuid():N}"),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(21),
                IsRevoked = false
            };
            _tokens.Add(token);
            return token;
        }
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        private readonly List<string> _sentLinks = new();

        public int SendCallCount { get; private set; }
        public string? LastToEmail { get; private set; }
        public string? LastLink => _sentLinks.Count == 0 ? null : _sentLinks[^1];
        public IReadOnlyList<string> SentLinks => _sentLinks;

        public Task SendAsync(string toEmail, string subject, string bodyHtml, CancellationToken cancellationToken = default)
        {
            SendCallCount++;
            LastToEmail = toEmail;
            var start = bodyHtml.IndexOf("http", StringComparison.Ordinal);
            var end = bodyHtml.IndexOf('"', start);
            _sentLinks.Add(bodyHtml[start..end]);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFrontendLinkBuilder : IFrontendLinkBuilder
    {
        public string BuildResetPasswordLink(string token) => $"http://localhost:4200/auth/reset-password-with-token?token={Uri.EscapeDataString(token)}";
    }

    // Real, working fakes (not throw-NotSupportedException stubs) — LoginAsync/RefreshTokenAsync now
    // genuinely call both methods. GenerateRefreshToken()'s raw value is Guid-backed to guarantee
    // distinctness across calls within a single test (rotation tests assert the new raw token
    // differs from the old one).
    private sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
    {
        private int _accessTokenCounter;

        public (string AccessToken, DateTime ExpiresAtUtc) GenerateAccessToken(AuthUserDto user) =>
            ($"fake-access-token-{++_accessTokenCounter}", DateTime.UtcNow.AddMinutes(15));

        public (string RawToken, DateTime ExpiresAtUtc) GenerateRefreshToken() =>
            ($"fake-refresh-token-{Guid.NewGuid():N}", DateTime.UtcNow.AddDays(21));
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeUnitOfWork(IUserRepository users, IPasswordResetTokenRepository passwordResetTokens, IRefreshTokenRepository refreshTokens)
        {
            Users = users;
            PasswordResetTokens = passwordResetTokens;
            RefreshTokens = refreshTokens;
        }

        public IUserRepository Users { get; }
        public IPasswordResetTokenRepository PasswordResetTokens { get; }
        public IRefreshTokenRepository RefreshTokens { get; }
        public IServiceRepository Services => throw new NotSupportedException();
        public IRoleRepository Roles => throw new NotSupportedException();
        public IPermissionRepository Permissions => throw new NotSupportedException();
        public IGroupRepository Groups => throw new NotSupportedException();
        public IProjectTypeRepository ProjectTypes => throw new NotSupportedException();
        public ICountryRepository Countries => throw new NotSupportedException();
        public ICityRepository Cities => throw new NotSupportedException();
        public ICityLocationRepository CityLocations => throw new NotSupportedException();
        public IAssociationProjectSupervisorRepository AssociationProjectSupervisors => throw new NotSupportedException();
        public IAssociationRepository Associations => throw new NotSupportedException();
        public IProductionCompanyRepository ProductionCompanies => throw new NotSupportedException();
        public IProjectRepository Projects => throw new NotSupportedException();
        public IWorkerRepository Workers => throw new NotSupportedException();
        public ISystemConfigurationRepository SystemConfigurations => throw new NotSupportedException();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

        public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default) => operation();
    }
}
