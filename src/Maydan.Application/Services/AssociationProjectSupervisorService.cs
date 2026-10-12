using Maydan.Application.DTOs;
using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Common;
using Maydan.Application.Interfaces;
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

    public async Task<ApiResponse<List<AssociationProjectSupervisorDto>>> GetAllAsync(int projectId, int associationId, CancellationToken cancellationToken = default)
    {
        var projectFilter = projectId > 0 ? projectId : (int?)null;
        var associationFilter = associationId > 0 ? associationId : (int?)null;

        var supervisors = await _unitOfWork.AssociationProjectSupervisors.GetAllAsync(projectFilter, associationFilter, cancellationToken);
        var associationNames = await ResolveAssociationNamesAsync(supervisors, cancellationToken);

        var dtos = supervisors.Select(s => MapToDto(s, associationNames)).ToList();

        return ApiResponse<List<AssociationProjectSupervisorDto>>.SuccessResponse(
            dtos,
            "Project supervisors retrieved successfully.",
            "تم جلب مشرفين المشاريع بنجاح."
        );
    }

    public async Task<ApiResponse<AssociationProjectSupervisorDto>> CreateAsync(CreateAssociationProjectSupervisorDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        var userValidation = await ValidateCurrentUserAsync(currentUserId, cancellationToken);
        if (!userValidation.IsSuccess)
        {
            return ApiResponse<AssociationProjectSupervisorDto>.FailureResponse(userValidation.MessageEn!, userValidation.MessageAr!);
        }

        var project = await _unitOfWork.Projects.GetByIdAsync(dto.ProjectId, cancellationToken);
        if (project is null)
        {
            return ApiResponse<AssociationProjectSupervisorDto>.FailureResponse("Project was not found.", "المشروع غير موجود.");
        }

        var supervisorUser = await _unitOfWork.Users.GetByIdAsync(dto.AssociationUserId, cancellationToken);
        if (supervisorUser is null)
        {
            return ApiResponse<AssociationProjectSupervisorDto>.FailureResponse("User was not found.", "المستخدم غير موجود.");
        }

        if (supervisorUser.EntityType != EntityType.Association)
        {
            return ApiResponse<AssociationProjectSupervisorDto>.FailureResponse("Supervisor must be an Association-role user.", "يجب أن يكون المشرف مستخدماً بصفة جمعية.");
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
        if (created is null)
        {
            return ApiResponse<AssociationProjectSupervisorDto>.FailureResponse("Supervisor assignment could not be loaded after creation.", "تعذر تحميل تعيين المشرف بعد الإنشاء.");
        }

        var associationNames = await ResolveAssociationNamesAsync(new[] { created }, cancellationToken);

        var resultDto = MapToDto(created, associationNames);

        return ApiResponse<AssociationProjectSupervisorDto>.SuccessResponse(
            resultDto,
            "Supervisor assigned successfully.",
            "تم تعيين المشرف بنجاح."
        );
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, int currentUserId, CancellationToken cancellationToken = default)
    {
        var userValidation = await ValidateCurrentUserAsync(currentUserId, cancellationToken);
        if (!userValidation.IsSuccess)
        {
            return ApiResponse<bool>.FailureResponse(userValidation.MessageEn!, userValidation.MessageAr!);
        }

        var supervisor = await _unitOfWork.AssociationProjectSupervisors.GetByIdAsync(id, cancellationToken);
        if (supervisor is null)
        {
            return ApiResponse<bool>.FailureResponse("Supervisor assignment was not found.", "تعيين المشرف غير موجود.");
        }

        await _unitOfWork.AssociationProjectSupervisors.Remove(supervisor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(
            true,
            "Supervisor assignment deleted successfully.",
            "تم حذف تعيين المشرف بنجاح."
        );
    }

    private async Task<(bool IsSuccess, string? MessageEn, string? MessageAr)> ValidateCurrentUserAsync(int currentUserId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        if (user is null)
        {
            return (false, "Current user was not found.", "المستخدم الحالي غير موجود.");
        }

        if (!user.IsActive)
        {
            return (false, "Current user is inactive.", "المستخدم الحالي غير نشط.");
        }

        return (true, null, null);
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