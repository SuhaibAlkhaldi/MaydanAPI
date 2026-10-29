using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.ProductionCompanies;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

public class ProductionCompanyService : IProductionCompanyService
{
    private const int ViewProductionCompaniesPermissionId = 19;
    private const int ManageProductionCompaniesPermissionId = 20;

    private readonly IUnitOfWork _unitOfWork;

    public ProductionCompanyService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<List<ProductionCompanyDto>>> GetAllAsync(int currentUserId, string? search, bool isDeleted, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await AuthorizeAsync<List<ProductionCompanyDto>>(currentUserId, ViewProductionCompaniesPermissionId, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var companies = await _unitOfWork.ProductionCompanies.QueryAsync(isDeleted, search, cancellationToken);
        var dtos = companies.Select(MapToDto).ToList();

        return ApiResponse<List<ProductionCompanyDto>>.SuccessResponse(
            dtos,
            "Production companies retrieved successfully.",
            "تم استرجاع قائمة شركات الإنتاج بنجاح.");
    }

    public async Task<ApiResponse<ProductionCompanyDto>> GetByIdAsync(int currentUserId, int productionCompanyId, CancellationToken cancellationToken = default)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken);
        if (currentUser is null)
        {
            return ApiResponse<ProductionCompanyDto>.FailureResponse("Current user was not found.", "المستخدم الحالي غير موجود.");
        }

        if (!currentUser.IsActive)
        {
            return ApiResponse<ProductionCompanyDto>.FailureResponse("Current user is inactive.", "المستخدم الحالي غير فعّال.");
        }

        var isOwnCompany = currentUser.EntityType == EntityType.ProductionCompany && currentUser.EntityId == productionCompanyId;
        if (!isOwnCompany && !GetEffectivePermissionIds(currentUser).Overlaps(new[] { ViewProductionCompaniesPermissionId, ManageProductionCompaniesPermissionId }))
        {
            return ApiResponse<ProductionCompanyDto>.FailureResponse("Caller does not hold the required Production Companies permission.", "لا تملك الصلاحية المطلوبة لعرض شركة الإنتاج.");
        }

        var company = await _unitOfWork.ProductionCompanies.GetByIdAsync(productionCompanyId, cancellationToken);
        if (company is null)
        {
            return ApiResponse<ProductionCompanyDto>.FailureResponse("Production company was not found.", "شركة الإنتاج غير موجودة.");
        }

        return ApiResponse<ProductionCompanyDto>.SuccessResponse(
            MapToDto(company),
            "Production company details retrieved successfully.",
            "تم استرجاع تفاصيل شركة الإنتاج بنجاح.");
    }

    public async Task<ApiResponse<ProductionCompanyDto>> UpdateStatusAsync(int currentUserId, int productionCompanyId, UpdateProductionCompanyStatusDto dto, CancellationToken cancellationToken = default)
        {
        var (currentUser, authError) = await AuthorizeAsync<ProductionCompanyDto>(currentUserId, ManageProductionCompaniesPermissionId, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var company = await _unitOfWork.ProductionCompanies.GetByIdAsync(productionCompanyId, cancellationToken);
        if (company is null)
        {
            return ApiResponse<ProductionCompanyDto>.FailureResponse("Production company was not found.", "شركة الإنتاج غير موجودة.");
        }

        company.IsActive = dto.IsActive;

        if (!dto.IsActive)
        {
            var users = await _unitOfWork.Users.GetByEntityAsync(EntityType.ProductionCompany, company.Id, search: null, cancellationToken);
            foreach (var user in users.Where(u => u.IsActive))
            {
                user.IsActive = false;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ProductionCompanyDto>.SuccessResponse(
            MapToDto(company),
            "Production company status updated successfully.",
            "تم تحديث حالة شركة الإنتاج بنجاح.");
    }

    #region Helpers

    private async Task<(User? User, ApiResponse<T>? Error)> AuthorizeAsync<T>(int currentUserId, int requiredPermissionId, CancellationToken cancellationToken)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken);
        if (currentUser is null)
        {
            return (null, ApiResponse<T>.FailureResponse("Current user was not found.", "المستخدم الحالي غير موجود."));
        }

        if (!currentUser.IsActive)
        {
            return (null, ApiResponse<T>.FailureResponse("Current user is inactive.", "المستخدم الحالي غير فعّال."));
        }

        if (!GetEffectivePermissionIds(currentUser).Overlaps(new[] { requiredPermissionId, ManageProductionCompaniesPermissionId }))
        {
            return (null, ApiResponse<T>.FailureResponse("Caller does not hold the required Production Companies permission.", "لا تملك الصلاحية المطلوبة لشركات الإنتاج."));
        }

        return (currentUser, null);
    }

    private static HashSet<int> GetEffectivePermissionIds(User user)
    {
        var rolePermissionIds = user.Role?.RolePermissions?
         .Where(rp => rp.IsActive && (rp.Permission == null || rp.Permission.IsActive))
         .Select(rp => rp.PermissionId) ?? Enumerable.Empty<int>();

        var directPermissionIds = user.UserPermissions?
            .Where(up => up.IsActive && (up.Permission == null || up.Permission.IsActive))
            .Select(up => up.PermissionId) ?? Enumerable.Empty<int>();

        var groupPermissionIds = user.UserGroups?
            .Where(ug => ug.Group != null && ug.Group.IsActive)
            .SelectMany(ug => ug.Group.GroupPermissions ?? new List<GroupPermission>())
            .Where(gp => gp.IsActive && (gp.Permission == null || gp.Permission.IsActive))
            .Select(gp => gp.PermissionId) ?? Enumerable.Empty<int>();

        return rolePermissionIds.Concat(directPermissionIds).Concat(groupPermissionIds).ToHashSet();
    }

    private static ProductionCompanyDto MapToDto(ProductionCompany company) => new()
    {
        Id = company.Id,
        EnglishName = company.EnglishName,
        ArabicName = company.ArabicName,
        RegistrationNumber = company.RegistrationNumber,
        CityId = company.CityId,
        CityEnglishName = company.City.EnglishName,
        CityArabicName = company.City.ArabicName,
        CountryId = company.City.Country.Id,
        CountryEnglishName = company.City.Country.EnglishName,
        CountryArabicName = company.City.Country.ArabicName,
        IsActive = company.IsActive,
        IsSelfRegistered = company.IsSelfRegistered,
        IsDeleted = company.IsDeleted
    };

    #endregion
}