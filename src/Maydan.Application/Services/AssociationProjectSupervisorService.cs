using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Common;
using Maydan.Application.Interfaces;
using Maydan.Domain.Constants;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

public class AssociationProjectSupervisorService : IAssociationProjectSupervisorService
{
    private readonly IUnitOfWork _unitOfWork;

    public AssociationProjectSupervisorService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<List<AssociationProjectSupervisorDto>>> GetAllAsync(int currentUserId, int projectId, int associationId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        if (user == null || !user.IsActive)
            return new ApiResponse<List<AssociationProjectSupervisorDto>>(false, "المستخدم الحالي غير مصرح له.", "Unauthorized.", default, 401);

 
        int? forcedAssociationId = associationId > 0 ? associationId : null;

        if (user.EntityType == EntityType.Association)
        {
            if (associationId > 0 && associationId != user.EntityId)
            {
                return new ApiResponse<List<AssociationProjectSupervisorDto>>(
                    false, "لا يمكنك استعراض بيانات جمعية أخرى.", "Forbidden: Cannot view data outside your association.", default, 403);
            }
            forcedAssociationId = user.EntityId;
        }

        if (projectId < 0)
        {
            return new ApiResponse<List<AssociationProjectSupervisorDto>>(
                        false, "المشروع غير موجود.", "Project was not found.", default, 404);

        }
        if (projectId > 0)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
            {
                return new ApiResponse<List<AssociationProjectSupervisorDto>>(
                    false, "المشروع غير موجود.", "Project was not found.", default, 404);
            }


            if (user.EntityType == EntityType.ProductionCompany && project.ProductionCompanyId != user.EntityId)
            {
                return new ApiResponse<List<AssociationProjectSupervisorDto>>(
                    false, "غير مصرح لك بالوصول لبيانات هذا المشروع.", "Forbidden: You do not have access to this project.", default, 403);
            }
        }


        if (forcedAssociationId > 0 && user.EntityType != EntityType.Association)
        {
            var association = await _unitOfWork.Associations.GetByIdAsync(forcedAssociationId.Value, cancellationToken);
            if (association == null)
            {
                return new ApiResponse<List<AssociationProjectSupervisorDto>>(
                    false, "الجمعية غير موجودة.", "Association was not found.", default, 404);
            }
        }

        var projectFilter = projectId > 0 ? projectId : (int?)null;


        var supervisors = await _unitOfWork.AssociationProjectSupervisors.GetAllAsync(projectFilter, forcedAssociationId, cancellationToken);


        if (user.EntityType == EntityType.ProductionCompany && projectId <= 0)
        {
           
            supervisors = supervisors.Where(s => s.Project != null && s.Project.ProductionCompanyId == user.EntityId).ToList();
        }

        if (projectId > 0 && user.EntityType == EntityType.Association && !supervisors.Any())
        {
            return new ApiResponse<List<AssociationProjectSupervisorDto>>(
                false, "غير مصرح لك بالوصول لبيانات هذا المشروع.", "Forbidden: You do not have access to this project.", default, 403);
        }

        var associationNames = await ResolveAssociationNamesAsync(supervisors, cancellationToken);
        var dtos = supervisors.Select(s => MapToDto(s, associationNames)).ToList();

        return ApiResponse<List<AssociationProjectSupervisorDto>>.SuccessResponse(dtos, "Supervisors retrieved successfully.", "تم استرجاع المشرفين بنجاح.");
    }

    public async Task<ApiResponse<AssociationProjectSupervisorDto>> CreateAsync(CreateAssociationProjectSupervisorDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null || !currentUser.IsActive)
            return new ApiResponse<AssociationProjectSupervisorDto>(false, "المستخدم الحالي غير مصرح له.", "Unauthorized.", default, 401);

        
        if (currentUser.EntityType == EntityType.ProductionCompany)
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
        //var allowedRoles = new[] { SystemRoles.Association, SystemRoles.WorkerManager, SystemRoles.Worker,SystemRoles.AttendanceOfficer };
        //if (supervisorUser.EntityType != EntityType.Association || !allowedRoles.Contains(supervisorUser.RoleId))
        //{
        //    return ApiResponse<AssociationProjectSupervisorDto>.FailureResponse(
        //        "Supervisor must be an Association user with an eligible supervisory role.",
        //        "يجب أن يكون المشرف مستخدماً تابعاً لجمعية ويمتلك صلاحية إشراف.");
        //}


        if (currentUser.EntityType == EntityType.Association && supervisorUser.EntityId != currentUser.EntityId)
        {
            return new ApiResponse<AssociationProjectSupervisorDto>(
                false, "لا يمكنك تعيين مشرف من جمعية أخرى.", "Forbidden: Cannot assign a supervisor from another association.", default, 403);
        }

        var existingSupervisors = await _unitOfWork.AssociationProjectSupervisors.GetAllAsync(dto.ProjectId, supervisorUser.EntityId, cancellationToken);
        if (existingSupervisors.Any(s => s.UserId == dto.AssociationUserId))
        {
            return ApiResponse<AssociationProjectSupervisorDto>.FailureResponse(
                "This supervisor is already assigned to this project.",
                "هذا المشرف معين مسبقاً على هذا المشروع.");
        }
        var allAssociationSupervisors = await _unitOfWork.AssociationProjectSupervisors.GetAllAsync(null, supervisorUser.EntityId, cancellationToken);
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

    public async Task<ApiResponse<object?>> DeleteAsync(int id, int currentUserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null || !currentUser.IsActive)
            return new ApiResponse<object?>(false, "المستخدم الحالي غير مصرح له.", "Unauthorized.", default, 401);

        if (currentUser.EntityType == EntityType.ProductionCompany)
        {
            return new ApiResponse<object?>(
                false, "شركة الإنتاج غير مخولة بحذف مشرفي الجمعيات.", "Forbidden: Production companies cannot delete association supervisors.", default, 403);
        }

        var supervisor = await _unitOfWork.AssociationProjectSupervisors.GetByIdAsync(id, cancellationToken);
        if (supervisor == null)
            return new ApiResponse<object?>(false, "المشرف غير موجود.", "Supervisor assignment was not found.", default, 404);

        // عزل الجمعية: لا تحذف إلا مشرفيها
        if (currentUser.EntityType == EntityType.Association && supervisor.User.EntityId != currentUser.EntityId)
        {
            return new ApiResponse<object?>(
                false, "غير مصرح لك بحذف مشرف يتبع لجمعية أخرى.", "Forbidden: Cannot delete a supervisor from another association.", default, 403);
        }

        _unitOfWork.AssociationProjectSupervisors.Remove(supervisor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object?>.SuccessResponse(null, "Supervisor assignment deleted successfully.", "تم حذف المشرف بنجاح.");
    }

    private async Task<ApiResponse<T>?> GetCurrentUserErrorAsync<T>(int currentUserId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        if (user == null)
            return new ApiResponse<T>(false, "المستخدم الحالي غير موجود.", "Current user was not found.", default, 401);

        if (!user.IsActive)
            return new ApiResponse<T>(false, "المستخدم الحالي غير فعّال.", "Current user is inactive.", default, 401);

        return null;
    }

    private async Task<Dictionary<int, string>> ResolveAssociationNamesAsync(IEnumerable<AssociationProjectSupervisor> supervisors, CancellationToken cancellationToken)
    {
        var associationIds = supervisors
            .Where(s => s.User.EntityType == EntityType.Association)
            .Select(s => s.User.EntityId)
            .Distinct();

        var associationNames = new Dictionary<int, string>();

        foreach (var associationId in associationIds)
        {
            var association = await _unitOfWork.Associations.GetByIdAsync(associationId, cancellationToken);
            if (association is not null)
            {
                associationNames[associationId] = association.EnglishName;
            }
        }

        return associationNames;
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
}