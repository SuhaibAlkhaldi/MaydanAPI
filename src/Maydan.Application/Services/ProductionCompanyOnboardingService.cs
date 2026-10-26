using Maydan.Application.DTOs.Auth;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

// Entity onboarding Stage 1 (2026-09-22): the only way today to create a brand-new
// ProductionCompany's first user. Deliberately its own service, not a method on AuthService or
// UserManagementService: it's neither an authentication concern (no session exists before this
// point — nothing to authenticate against) nor an entity-scoped user-management concern
// (UserManagementService.CreateUserAsync's EnsureSameEntityCreation guard assumes a currentUser to
// inherit EntityType/EntityId from, which doesn't exist here). Production companies only for now —
// Associations/ASEZA/Bayt-AlUrdon onboarding is Stage 2+, a different flow per the confirmed
// product decisions, not this class's concern.
public class ProductionCompanyOnboardingService : IProductionCompanyOnboardingService
{
    // Matches RolePermissionSeedConfiguration.cs's ProductionHouse const (RoleId 3) — the new
    // admin's role. Not resolved by name lookup: this ticket's confirmed flow only ever creates a
    // ProductionCompany admin, so the seeded id is the right thing to pin against, same as how
    // UserManagementServiceTests pins RoleId literals in its own fixtures.
    private const int ProductionHouseRoleId = 3;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public ProductionCompanyOnboardingService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterProductionCompanyResponseDto> RegisterAsync(RegisterProductionCompanyDto dto, CancellationToken cancellationToken = default)
    {
        ValidatePayload(dto);

        var email = dto.Email.Trim();
        if (await _unitOfWork.Users.EmailExistsAsync(email, cancellationToken))
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var registrationNumber = dto.RegistrationNumber.Trim();
        if (await _unitOfWork.ProductionCompanies.RegistrationNumberExistsAsync(registrationNumber, cancellationToken))
        {
            throw new InvalidOperationException("A production company with this registration number is already registered.");
        }

        // MAYD-79 gap fix: the ticket's own text requires CompanyNameEn to be unique, same as
        // RegistrationNumber — this check never existed until now (confirmed by reading this
        // method before the fix; only Email and RegistrationNumber were ever checked).
        var companyNameEn = dto.CompanyNameEn.Trim();
        if (await _unitOfWork.ProductionCompanies.EnglishNameExistsAsync(companyNameEn, cancellationToken))
        {
            throw new InvalidOperationException("A production company with this English name is already registered.");
        }

        if (await _unitOfWork.Cities.GetByIdAsync(dto.CityId, cancellationToken) is null)
        {
            throw new KeyNotFoundException("City was not found.");
        }

        var role = await _unitOfWork.Roles.GetWithPermissionsAsync(ProductionHouseRoleId, cancellationToken)
            ?? throw new KeyNotFoundException("ProductionHouse role was not found.");

        var phone = $"{dto.MobileCountryCode.Trim()}{dto.MobileNumber.Trim()}";

        // Stage 3: real Arabic name fields arrived on the DTO — the Stage 1 stopgap that mirrored
        // the Latin name into FirstNameAr/LastNameAr (to satisfy User.cs's NOT NULL columns without
        // a real Arabic value) is gone.
        var firstNameEn = dto.AdminFirstName.Trim();
        var lastNameEn = dto.AdminLastName.Trim();
        var firstNameAr = dto.AdminFirstNameAr.Trim();
        var lastNameAr = dto.AdminLastNameAr.Trim();

        var company = new ProductionCompany
        {
            EnglishName = companyNameEn,
            ArabicName = dto.CompanyNameAr.Trim(),
            RegistrationNumber = registrationNumber,
            CityId = dto.CityId,
            // No separate company-contact fields exist on this form — the registering admin is the
            // company's point of contact at signup time, so these default from the admin's own
            // details rather than staying empty. Revisit if a later stage adds dedicated fields.
            ContactEmail = email,
            ContactPhone = phone,
            IsSelfRegistered = true,
            IsActive = true
        };

        var user = new User
        {
            FirstNameEn = firstNameEn,
            LastNameEn = lastNameEn,
            FirstNameAr = firstNameAr,
            LastNameAr = lastNameAr,
            Email = email,
            PhoneNumber = phone,
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
            // The wizard (UserManagementService.CreateUserAsync) always forces true for
            // employees an admin creates with a temp password. This admin chose their own
            // password during registration, so there's nothing to force a reset of.
            MustResetPassword = false,
            IsActive = true,
            RoleId = role.RoleId,
            EntityType = EntityType.ProductionCompany
        };

        foreach (var rolePermission in role.RolePermissions.Where(rp => rp.IsActive && rp.Permission.IsActive))
        {
            user.UserPermissions.Add(new UserPermission { PermissionId = rolePermission.PermissionId, IsActive = true });
        }

        // Two SaveChangesAsync calls, not one: see IUnitOfWork.ExecuteInTransactionAsync's comment
        // on why User.EntityId (a soft FK) can't be populated before ProductionCompany.Id exists.
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _unitOfWork.ProductionCompanies.AddAsync(company, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            user.EntityId = company.Id;
            await _unitOfWork.Users.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return new RegisterProductionCompanyResponseDto(
            company.Id,
            company.EnglishName,
            company.ArabicName,
            user.UserId,
            user.Email,
            "Production company registered successfully. You can now log in.");
    }

    private static void ValidatePayload(RegisterProductionCompanyDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CompanyNameEn) ||
            string.IsNullOrWhiteSpace(dto.CompanyNameAr) ||
            string.IsNullOrWhiteSpace(dto.RegistrationNumber) ||
            string.IsNullOrWhiteSpace(dto.AdminFirstName) ||
            string.IsNullOrWhiteSpace(dto.AdminLastName) ||
            string.IsNullOrWhiteSpace(dto.AdminFirstNameAr) ||
            string.IsNullOrWhiteSpace(dto.AdminLastNameAr) ||
            string.IsNullOrWhiteSpace(dto.MobileCountryCode) ||
            string.IsNullOrWhiteSpace(dto.MobileNumber) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new InvalidOperationException("All company and admin fields are required.");
        }

        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(dto.Email))
        {
            throw new InvalidOperationException("Invalid email address format.");
        }

        if (dto.CityId <= 0)
        {
            throw new InvalidOperationException("A valid city is required.");
        }

        // Mirrors the frontend's own minlength(8) — a floor, not a re-implementation of its full
        // uppercase+symbol strength check, which is presentation-layer UX rather than a security
        // boundary worth duplicating server-side.
        if (dto.Password.Length < 8)
        {
            throw new InvalidOperationException("Password must be at least 8 characters.");
        }
    }
}
