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

        return Ok(dtos, "تم استرجاع قائمة شركات الإنتاج بنجاح.", "Production companies retrieved successfully.");
    }

    public async Task<ApiResponse<ProductionCompanyDto>> GetByIdAsync(int currentUserId, int productionCompanyId, CancellationToken cancellationToken = default)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken);
        if (currentUser is null)
        {
            return Fail<ProductionCompanyDto>("المستخدم الحالي غير موجود.", "Current user was not found.");
        }

        if (!currentUser.IsActive)
        {
            return Fail<ProductionCompanyDto>("المستخدم الحالي غير فعّال.", "Current user is inactive.");
        }

        var isOwnCompany = currentUser.EntityType == EntityType.ProductionCompany && currentUser.EntityId == productionCompanyId;
        if (!isOwnCompany && !GetEffectivePermissionIds(currentUser).Overlaps(new[] { ViewProductionCompaniesPermissionId, ManageProductionCompaniesPermissionId }))
        {
            return Fail<ProductionCompanyDto>("لا تملك الصلاحية المطلوبة لعرض شركة الإنتاج.", "Caller does not hold the required Production Companies permission.");
        }

        var company = await _unitOfWork.ProductionCompanies.GetByIdAsync(productionCompanyId, cancellationToken);
        if (company is null)
        {
            return Fail<ProductionCompanyDto>("شركة الإنتاج غير موجودة.", "Production company was not found.");
        }

        return Ok(MapToDto(company), "تم استرجاع تفاصيل شركة الإنتاج بنجاح.", "Production company details retrieved successfully.");
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
            return Fail<ProductionCompanyDto>("شركة الإنتاج غير موجودة.", "Production company was not found.");
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

        return Ok(MapToDto(company), "تم تحديث حالة شركة الإنتاج بنجاح.", "Production company status updated successfully.");
    }

    #region Helpers

    private async Task<(User? User, ApiResponse<T>? Error)> AuthorizeAsync<T>(int currentUserId, int requiredPermissionId, CancellationToken cancellationToken)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken);
        if (currentUser is null)
        {
            return (null, Fail<T>("المستخدم الحالي غير موجود.", "Current user was not found."));
        }

        if (!currentUser.IsActive)
        {
            return (null, Fail<T>("المستخدم الحالي غير فعّال.", "Current user is inactive."));
        }

        if (!GetEffectivePermissionIds(currentUser).Overlaps(new[] { requiredPermissionId, ManageProductionCompaniesPermissionId }))
        {
            return (null, Fail<T>("لا تملك الصلاحية المطلوبة لشركات الإنتاج.", "Caller does not hold the required Production Companies permission."));
        }

        return (currentUser, null);
    }

    private static HashSet<int> GetEffectivePermissionIds(User user)
    {
        var rolePermissionIds = user.Role.RolePermissions
            .Where(rp => rp.IsActive && rp.Permission.IsActive)
            .Select(rp => rp.PermissionId);

        var directPermissionIds = user.UserPermissions
            .Where(up => up.IsActive && up.Permission.IsActive)
            .Select(up => up.PermissionId);

        var groupPermissionIds = user.UserGroups
            .Where(ug => ug.Group.IsActive)
            .SelectMany(ug => ug.Group.GroupPermissions)
            .Where(gp => gp.IsActive && gp.Permission.IsActive)
            .Select(gp => gp.PermissionId);

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

    private static ApiResponse<T> Fail<T>(string messageAr, string messageEn, int statusCode = 400) =>
        new(false, messageAr, messageEn, default, statusCode);

    private static ApiResponse<T> Ok<T>(T data, string messageAr, string messageEn, int statusCode = 200) =>
        new(true, messageAr, messageEn, data, statusCode);

    #endregion
}