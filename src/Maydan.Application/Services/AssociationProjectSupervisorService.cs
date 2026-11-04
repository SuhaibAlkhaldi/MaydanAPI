using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Common;
using Maydan.Application.Interfaces;
using Maydan.Domain.Constants;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

public class AssociationProjectSupervisorService : IAssociationProjectSupervisorService
{
    // الصلاحية المطلوبة للجمعية لتعيين وحذف المشرفين التابعين لها
    private const int ManageAssociationUsersPermissionId = 39;

    private readonly IUnitOfWork _unitOfWork;

    public AssociationProjectSupervisorService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<PagedAssociationProjectSupervisorDto>> GetAllAsync(
        int currentUserId, int projectId, int associationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        if (user == null || !user.IsActive)
            return new ApiResponse<PagedAssociationProjectSupervisorDto>(false, "المستخدم الحالي غير مصرح له.", "Unauthorized.", default, 401);

        int? forcedAssociationId = associationId > 0 ? associationId : null;

        if (user.EntityType == EntityType.Association)
        {
            if (associationId > 0 && associationId != user.EntityId)
                return new ApiResponse<PagedAssociationProjectSupervisorDto>(false, "لا يمكنك استعراض بيانات جمعية أخرى.", "Forbidden.", default, 403);

            forcedAssociationId = user.EntityId;
        }

        if (projectId < 0)
            return new ApiResponse<PagedAssociationProjectSupervisorDto>(false, "المشروع غير موجود.", "Project was not found.", default, 404);

        if (projectId > 0)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
                return new ApiResponse<PagedAssociationProjectSupervisorDto>(false, "المشروع غير موجود.", "Project was not found.", default, 404);

            if (user.EntityType == EntityType.ProductionCompany && project.ProductionCompanyId != user.EntityId)
                return new ApiResponse<PagedAssociationProjectSupervisorDto>(false, "غير مصرح لك بالوصول لبيانات هذا المشروع.", "Forbidden.", default, 403);
        }

        int? forcedProductionCompanyId = (user.EntityType == EntityType.ProductionCompany && projectId <= 0)
            ? user.EntityId
            : null;

        var projectFilter = projectId > 0 ? projectId : (int?)null;
        var allowedRoles = new[] { SystemRoles.Association_Representative };
        // تمرير الفلتر للريبوزيتوري ليتم بالداتابيس
        var (supervisors, totalCount) = await _unitOfWork.AssociationProjectSupervisors.GetAllAsync(
            projectFilter, forcedAssociationId, forcedProductionCompanyId, page, pageSize, allowedRoles, cancellationToken);

        if (projectId > 0 && user.EntityType == EntityType.Association && totalCount == 0)
        {
            return new ApiResponse<PagedAssociationProjectSupervisorDto>(false, "غير مصرح لك بالوصول لبيانات هذا المشروع.", "Forbidden.", default, 403);
        }

        var associationNames = await ResolveAssociationNamesAsync(supervisors, cancellationToken);
        var dtos = supervisors.Select(s => MapToDto(s, associationNames)).ToList();

        var pagedResult = new PagedAssociationProjectSupervisorDto
        {
            Items = dtos,
            TotalCount = totalCount
        };

        return ApiResponse<PagedAssociationProjectSupervisorDto>.SuccessResponse(pagedResult, "Supervisors retrieved successfully.", "تم استرجاع المشرفين بنجاح.");
    }

    public async Task<ApiResponse<List<AssociationProjectSupervisorDto>>> GetByProjectIdAsync(
        int projectId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        if (user == null || !user.IsActive)
            return new ApiResponse<List<AssociationProjectSupervisorDto>>(false, "المستخدم الحالي غير مصرح له.", "Unauthorized.", default, 401);

        var project = await _unitOfWork.Projects.GetByIdAsync(projectId, cancellationToken);
        if (project == null)
            return new ApiResponse<List<AssociationProjectSupervisorDto>>(false, "المشروع غير موجود.", "Project was not found.", default, 404);

        if (user.EntityType == EntityType.ProductionCompany && project.ProductionCompanyId != user.EntityId)
            return new ApiResponse<List<AssociationProjectSupervisorDto>>(false, "غير مصرح لك بالوصول لبيانات هذا المشروع.", "Forbidden.", default, 403);

        var supervisors = await _unitOfWork.AssociationProjectSupervisors.GetByProjectIdAsync(projectId, cancellationToken);

        if (user.EntityType == EntityType.Association)
        {
            supervisors = supervisors.Where(s => s.User.EntityType == EntityType.Association && s.User.EntityId == user.EntityId).ToList();
        }

        var associationNames = await ResolveAssociationNamesAsync(supervisors, cancellationToken);
        var dtos = supervisors.Select(s => MapToDto(s, associationNames)).ToList();

        return ApiResponse<List<AssociationProjectSupervisorDto>>.SuccessResponse(dtos, "Supervisors retrieved successfully.", "تم استرجاع المشرفين بنجاح.");
    }

    public async Task<ApiResponse<AssociationProjectSupervisorDto>> GetByIdAsync(
        int id, int currentUserId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        if (user == null || !user.IsActive)
            return new ApiResponse<AssociationProjectSupervisorDto>(false, "المستخدم الحالي غير مصرح له.", "Unauthorized.", default, 401);

        var supervisor = await _unitOfWork.AssociationProjectSupervisors.GetByIdAsync(id, cancellationToken);
        if (supervisor == null)
            return new ApiResponse<AssociationProjectSupervisorDto>(false, "المشرف غير موجود.", "Supervisor assignment was not found.", default, 404);

        if (user.EntityType == EntityType.ProductionCompany && supervisor.Project != null && supervisor.Project.ProductionCompanyId != user.EntityId)
            return new ApiResponse<AssociationProjectSupervisorDto>(false, "غير مصرح لك بالوصول لبيانات هذا المشرف.", "Forbidden.", default, 403);

        if (user.EntityType == EntityType.Association && (supervisor.User.EntityType != EntityType.Association || supervisor.User.EntityId != user.EntityId))
            return new ApiResponse<AssociationProjectSupervisorDto>(false, "غير مصرح لك بالوصول لبيانات هذا المشرف.", "Forbidden.", default, 403);

        var associationNames = await ResolveAssociationNamesAsync(new[] { supervisor }, cancellationToken);
        return ApiResponse<AssociationProjectSupervisorDto>.SuccessResponse(MapToDto(supervisor, associationNames), "Supervisor retrieved successfully.", "تم استرجاع المشرف بنجاح.");
    }

    public async Task<ApiResponse<AssociationProjectSupervisorDto>> CreateAsync(CreateAssociationProjectSupervisorDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        // استخدام AuthorizeAsync لفحص الصلاحية 39
        var (currentUser, authError) = await AuthorizeAsync<AssociationProjectSupervisorDto>(
            currentUserId,
            new[] { ManageAssociationUsersPermissionId },
            cancellationToken);

        if (authError is not null)
        {
            return authError;
        }

        if (currentUser!.EntityType == EntityType.ProductionCompany)
        {
            return new ApiResponse<AssociationProjectSupervisorDto>(
                false, "شركة الإنتاج غير مخولة بتعيين مشرفي الجمعيات.", "Forbidden: Production companies cannot assign association supervisors.", default, 403);
        }

        var project = await _unitOfWork.Projects.GetByIdAsync(dto.ProjectId, cancellationToken);
        if (project == null)
            return new ApiResponse<AssociationProjectSupervisorDto>(false, "المشروع غير موجود.", "Project was not found.", default, 404);

        var supervisorUser = await _unitOfWork.Users.GetByIdAsync(dto.AssociationUserId, cancellationToken);
        if (supervisorUser == null)
            return new ApiResponse<AssociationProjectSupervisorDto>(false, "المستخدم غير موجود.", "User was not found.", default, 404);

        // pleaseeee keep this code here cause we need to know if the client want to assing specific roles to project supervisors
        // supervisor must be a 4 roles in assonciation 
        var allowedRoles = new[] { SystemRoles.Association_Representative };
        if (supervisorUser.EntityType != EntityType.Association || !allowedRoles.Contains(supervisorUser.RoleId))
        {
            return ApiResponse<AssociationProjectSupervisorDto>.FailureResponse(
                "Supervisor must be an Association user with an eligible supervisory role.",
                "يجب أن يكون المشرف مستخدماً تابعاً لجمعية ويمتلك صلاحية إشراف.");
        }

        if (currentUser.EntityType == EntityType.Association && supervisorUser.EntityId != currentUser.EntityId)
        {
            return new ApiResponse<AssociationProjectSupervisorDto>(
                false, "لا يمكنك تعيين مشرف من جمعية أخرى.", "Forbidden: Cannot assign a supervisor from another association.", default, 403);
        }

        // التعديل الأول: استدعاء الريبوزيتوري مع تفكيك الـ Tuple وتمرير متغيرات الباجنيشن
        var (existingSupervisors, _) = await _unitOfWork.AssociationProjectSupervisors.GetAllAsync(dto.ProjectId, supervisorUser.EntityId, null, 1, int.MaxValue, allowedRoles, cancellationToken);
        if (existingSupervisors.Any(s => s.UserId == dto.AssociationUserId))
        {
            return ApiResponse<AssociationProjectSupervisorDto>.FailureResponse(
                "This supervisor is already assigned to this project.",
                "هذا المشرف معين مسبقاً على هذا المشروع.");
        }

        // التعديل الثاني: استدعاء الريبوزيتوري لفحص تعارض الأوقات
        var (allAssociationSupervisors, _) = await _unitOfWork.AssociationProjectSupervisors.GetAllAsync(null, supervisorUser.EntityId, null, 1, int.MaxValue, allowedRoles, cancellationToken);
        var userAssignments = allAssociationSupervisors.Where(s => s.UserId == dto.AssociationUserId).ToList();
        bool hasTimeConflict = userAssignments.Any(a =>
            a.Project != null &&
            project.StartDate <= a.Project.EndDate &&
            project.EndDate >= a.Project.StartDate);

        if (hasTimeConflict)
        {
            return ApiResponse<AssociationProjectSupervisorDto>.FailureResponse(
                "Time conflict: The user is assigned to another project during this period.",
                "يوجد تعارض في الوقت: هذا المستخدم معين على مشروع آخر في نفس الفترة الزمنية.");
        }

        var supervisor = new AssociationProjectSupervisor
        {
            ProjectId = project.Id,
            Project = project,
            UserId = supervisorUser.UserId,
            User = supervisorUser,
            CreatedBy = currentUserId,
            IsActive = true
        };

        await _unitOfWork.AssociationProjectSupervisors.AddAsync(supervisor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _unitOfWork.AssociationProjectSupervisors.GetByIdAsync(supervisor.Id, cancellationToken);
        if (created == null)
            return new ApiResponse<AssociationProjectSupervisorDto>(false, "تعذر تحميل المشرف بعد الإنشاء.", "Supervisor assignment could not be loaded after creation.", default, 500);

        var associationNames = await ResolveAssociationNamesAsync(new[] { created }, cancellationToken);

        return ApiResponse<AssociationProjectSupervisorDto>.SuccessResponse(MapToDto(created, associationNames), "Supervisor assigned successfully.", "تم تعيين المشرف بنجاح.", 201);
    }

    public async Task<ApiResponse<object?>> DeleteAsync(int supervisorId, int currentUserId, CancellationToken cancellationToken = default)
    {
        // استخدام AuthorizeAsync لفحص الصلاحية 39
        var (currentUser, authError) = await AuthorizeAsync<object?>(
            currentUserId,
            new[] { ManageAssociationUsersPermissionId },
            cancellationToken);

        if (authError is not null)
        {
            return authError;
        }

        var supervisorRecord = await _unitOfWork.AssociationProjectSupervisors.GetByIdAsync(supervisorId, cancellationToken);
        if (supervisorRecord == null)
            return new ApiResponse<object?>(false, "سجل الإشراف غير موجود.", "Supervisor record not found.", default, 404);

        var allowedRoles = new[] { SystemRoles.Association_Representative };

        if (supervisorRecord.User == null || !allowedRoles.Contains(supervisorRecord.User.RoleId))
        {
            return ApiResponse<object?>.FailureResponse(
                "Target user does not have an eligible supervisory role.",
                "المستخدم المستهدف لا يمتلك صلاحية إشراف صالحة حالياً.");
        }

        if (currentUser!.EntityType == EntityType.Association && supervisorRecord.User.EntityId != currentUser.EntityId)
        {
            return new ApiResponse<object?>(false, "لا يمكنك حذف مشرف من جمعية أخرى.", "Forbidden.", default, 403);
        }
        else if (currentUser.EntityType == EntityType.ProductionCompany)
        {
            return new ApiResponse<object?>(false, "شركة الإنتاج غير مخولة بحذف مشرفي الجمعيات.", "Forbidden.", default, 403);
        }

        _unitOfWork.AssociationProjectSupervisors.Remove(supervisorRecord);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object?>.SuccessResponse(null, "Supervisor removed successfully.", "تم حذف المشرف بنجاح.");
    }

    #region Helpers

    private async Task<(User? User, ApiResponse<T>? Error)> AuthorizeAsync<T>(
        int currentUserId,
        int[] requiredPermissionIds,
        CancellationToken cancellationToken)
    {
        // نستخدم GetWithPermissionsAsync عشان نجيب الصلاحيات الحقيقية لليوزر
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken);
        if (currentUser is null)
        {
            return (null, ApiResponse<T>.FailureResponse("Current user was not found.", "المستخدم الحالي غير موجود."));
        }

        if (!currentUser.IsActive)
        {
            return (null, ApiResponse<T>.FailureResponse("Current user is inactive.", "المستخدم الحالي غير فعّال."));
        }

        if (!GetEffectivePermissionIds(currentUser).Overlaps(requiredPermissionIds))
        {
            return (null, ApiResponse<T>.FailureResponse("Caller does not hold the required permission to manage association supervisors.", "لا تملك الصلاحية المطلوبة لإدارة مشرفي الجمعية."));
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

    private async Task<Dictionary<int, string>> ResolveAssociationNamesAsync(IEnumerable<AssociationProjectSupervisor> supervisors, CancellationToken cancellationToken)
    {
        var associationIds = supervisors
            .Where(s => s.User != null && s.User.EntityType == EntityType.Association)
            .Select(s => s.User.EntityId)
            .Distinct()
            .ToList();

        if (!associationIds.Any())
            return new Dictionary<int, string>();

        // ضربة وحدة عالداتابيس بتجيب كل الجمعيات المطلوبة
        var associations = await _unitOfWork.Associations.GetByIdsAsync(associationIds, cancellationToken);

        // تحويل النتيجة لـ Dictionary في الـ Memory
        return associations.ToDictionary(
            a => a.Id,
            a => a.EnglishName
        );
    }

    private static AssociationProjectSupervisorDto MapToDto(AssociationProjectSupervisor supervisor, IReadOnlyDictionary<int, string> associationNames)
    {
        var isAssociationUser = supervisor.User.EntityType == EntityType.Association;
        var associationId = isAssociationUser ? supervisor.User.EntityId : (int?)null;

        return new AssociationProjectSupervisorDto
        {
            Id = supervisor.Id,
            AssociationUserId = supervisor.UserId,
            ProjectId = supervisor.ProjectId,
            AssociationId = associationId,
            ProjectName = supervisor.Project.ProjectNameEn,
            AssociationName = associationId is int id && associationNames.TryGetValue(id, out var name) ? name : null,
            UserName = $"{supervisor.User.FirstNameEn} {supervisor.User.LastNameEn}".Trim(),
            Email = supervisor.User.Email,
            PhoneNumber = supervisor.User.PhoneNumber
        };
    }

    #endregion
}