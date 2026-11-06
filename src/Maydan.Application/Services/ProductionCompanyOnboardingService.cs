using Maydan.Application.Common;
using Maydan.Application.DTOs.Auth;
using Maydan.Application.DTOs.Common;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;
using System.Text.RegularExpressions;



namespace Maydan.Application.Services
{
    public class ProductionCompanyOnboardingService : IProductionCompanyOnboardingService
    {
        private const int ProductionHouseRoleId = 3;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public ProductionCompanyOnboardingService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        // 1. مسار التسجيل الذاتي (دائماً غير مفعل)
        public Task<ApiResponse<RegisterProductionCompanyResponseDto>> RegisterAsync(
            RegisterProductionCompanyDto dto,
            CancellationToken cancellationToken = default)
        {
            return ExecuteOnboardingAsync(dto, isCompanyActive: false, isSelfRegistered: true, adminUserId: null, cancellationToken);
        }

        // 2. مسار الإنشاء المباشر من الأدمن (دائماً مفعل، ويسجل معرف الأدمن)
        public Task<ApiResponse<RegisterProductionCompanyResponseDto>> CreateByAdminAsync(
            RegisterProductionCompanyDto dto,
            int adminUserId,
            CancellationToken cancellationToken = default)
        {
            return ExecuteOnboardingAsync(dto, isCompanyActive: true, isSelfRegistered: false, adminUserId: adminUserId, cancellationToken);
        }

        // الدالة الداخلية المشتركة لمنع تكرار الكود
        private async Task<ApiResponse<RegisterProductionCompanyResponseDto>> ExecuteOnboardingAsync(
            RegisterProductionCompanyDto dto,
            bool isCompanyActive,
            bool isSelfRegistered,
            int? adminUserId,
            CancellationToken cancellationToken)
        {

            var validationError = ValidatePayload(
                dto,
                out var phone);

            if (validationError != null)
            {
                return validationError;
            }


            var email = dto.Email.Trim();
            if (await _unitOfWork.Users.EmailExistsAsync(email, cancellationToken))
            {
                return ApiResponse<RegisterProductionCompanyResponseDto>.FailureResponse("A user with this email already exists.", "يوجد مستخدم مسجل بهذا البريد الإلكتروني مسبقاً.");
            }

            var registrationNumber = dto.RegistrationNumber.Trim();
            if (await _unitOfWork.ProductionCompanies.RegistrationNumberExistsAsync(registrationNumber, cancellationToken))
            {
                return ApiResponse<RegisterProductionCompanyResponseDto>.FailureResponse("A production company with this registration number is already registered.", "شركة الإنتاج بهذا رقم التسجيل مسجلة مسبقاً.");
            }

            var companyNameEn = dto.CompanyNameEn.Trim();
            if (await _unitOfWork.ProductionCompanies.EnglishNameExistsAsync(companyNameEn, cancellationToken))
            {
                return ApiResponse<RegisterProductionCompanyResponseDto>.FailureResponse("A production company with this English name is already registered.", "شركة الإنتاج بهذا الاسم الإنجليزي مسجلة مسبقاً.");
            }

            if (await _unitOfWork.Cities.GetByIdAsync(dto.CityId, cancellationToken) is null)
            {
                return ApiResponse<RegisterProductionCompanyResponseDto>.FailureResponse("City was not found.", "المدينة غير موجودة.");
            }

            var role = await _unitOfWork.Roles.GetWithPermissionsAsync(ProductionHouseRoleId, cancellationToken);
            if (role == null)
            {
                return ApiResponse<RegisterProductionCompanyResponseDto>.FailureResponse("ProductionHouse role was not found.", "دور شركة الإنتاج غير موجود بالنظام.");
            }

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
                ContactEmail = email,
                ContactPhone = phone,
                IsSelfRegistered = isSelfRegistered,
                IsActive = isCompanyActive,
                CreatedByUserId = adminUserId,   // يأخذ رقم الأدمن إذا كان إنشاء مباشر، أو null إذا تسجيل ذاتي
                ApprovedByUserId = adminUserId   // لأنه تفعل مباشرة على يد هذا الأدمن
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
                MustResetPassword = false,
                IsActive = isCompanyActive,
                RoleId = role.RoleId,
                EntityType = EntityType.ProductionCompany
            };

            foreach (var rolePermission in role.RolePermissions.Where(rp => rp.IsActive && rp.Permission.IsActive))
            {
                user.UserPermissions.Add(new UserPermission { PermissionId = rolePermission.PermissionId, IsActive = true });
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _unitOfWork.ProductionCompanies.AddAsync(company, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                user.EntityId = company.Id;
                await _unitOfWork.Users.AddAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }, cancellationToken);

            var responseDto = new RegisterProductionCompanyResponseDto(
                company.Id,
                company.EnglishName,
                company.ArabicName,
                user.UserId,
                user.Email,
                isCompanyActive ? "Production company registered and activated successfully." : "Production company registered successfully. Pending admin approval.");

            string successMsgEn = isCompanyActive
                ? "Registration successful. The production company is activated."
                : "Registration successful. Your account is pending admin approval.";

            string successMsgAr = isCompanyActive
                ? "تم تسجيل وتفعيل شركة الإنتاج بنجاح."
                : "تم تسجيل الشركة بنجاح. حسابك بانتظار موافقة الإدارة لتفعيله.";

            return ApiResponse<RegisterProductionCompanyResponseDto>.SuccessResponse(
                responseDto,
                successMsgEn,
                successMsgAr,
                201);
        }

        private static ApiResponse<RegisterProductionCompanyResponseDto>? ValidatePayload(RegisterProductionCompanyDto dto, out string normalizedPhoneNumber)
        {

            normalizedPhoneNumber = string.Empty;
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
                return ApiResponse<RegisterProductionCompanyResponseDto>.FailureResponse("All company and admin fields are required.", "جميع حقول الشركة والمسؤول مطلوبة.");
            }

            if (dto.CityId <= 0)
            {
                return ApiResponse<RegisterProductionCompanyResponseDto>.FailureResponse("A valid city is required.", "يجب اختيار مدينة صحيحة.");
            }

            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (!emailRegex.IsMatch(dto.Email.Trim()))
            {
                return ApiResponse<RegisterProductionCompanyResponseDto>.FailureResponse("Invalid email format.", "صيغة البريد الإلكتروني غير صحيحة.");
            }


            if (dto.MobileCountryCode.Trim() != "+962")
            {
                return ApiResponse<RegisterProductionCompanyResponseDto>
                    .FailureResponse(
                        "Country code must be +962.",
                        "يجب أن يكون رمز الدولة +962.");
            }

            var combinedPhone =
                $"{dto.MobileCountryCode.Trim()}{dto.MobileNumber.Trim()}";

            if (!JordanianPhoneNumberHelper.TryNormalize(
                    combinedPhone,
                    out normalizedPhoneNumber))
            {
                return ApiResponse<RegisterProductionCompanyResponseDto>
                    .FailureResponse(
                        "Invalid Jordanian mobile number.",
                        "رقم الهاتف غير صالح، يجب إدخال رقم أردني صحيح بعد +962.");
            }


            var regNumRegex = new Regex(@"^[a-zA-Z0-9\-]{5,15}$", RegexOptions.Compiled);
            if (!regNumRegex.IsMatch(dto.RegistrationNumber.Trim()))
            {
                return ApiResponse<RegisterProductionCompanyResponseDto>.FailureResponse(
                    "Invalid registration number. Must contain letters and numbers only (5 to 15 characters).",
                    "رقم التسجيل غير صحيح. يجب أن يحتوي على حروف وأرقام فقط (من 5 إلى 15 خانة).");
            }

            if (dto.Password.Length < 8)
            {
                return ApiResponse<RegisterProductionCompanyResponseDto>.FailureResponse("Password must be at least 8 characters.", "يجب أن تتكون كلمة المرور من 8 أحرف على الأقل.");
            }

            return null;
        }
    }
}