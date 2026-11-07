using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Projects;
using Maydan.Application.Interfaces;
using Maydan.Domain.Constants;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

/// <summary>
/// EN: Implements business logic and strict security controls for ProjectWorkers.
/// AR: ينفذ منطق الأعمال والضوابط الأمنية الصارمة لإدارة عمال المشروع.
/// </summary>
public class ProjectWorkerService : IProjectWorkerService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProjectWorkerService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SECURITY & ACCESS CONTROL HELPER (غيث: صلاحية مطلقة | أسيزا: رقابي فقط View-Only)
    // ──────────────────────────────────────────────────────────────────────────
    // ──────────────────────────────────────────────────────────────────────────
    // SECURITY & ACCESS CONTROL HELPER (غيث: صلاحية مطلقة | أسيزا: رقابي فقط View-Only)
    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// EN: Evaluates user permissions using SystemRoles for read vs. write operations and enforces tenant isolation.
    /// AR: يفحص صلاحيات المستخدم باستخدام ثوابت النظام لعمليات القراءة مقابل التعديل ويفرض عزل البيانات بين الجهات.
    /// </summary>
   // ──────────────────────────────────────────────────────────────────────────
    // SECURITY & ACCESS CONTROL HELPER (غيث: صلاحية مطلقة | أسيزا: رقابي فقط View-Only)
    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// EN: Evaluates user permissions using SystemRoles for read vs. write operations and enforces tenant isolation.
    /// AR: يفحص صلاحيات المستخدم باستخدام ثوابت النظام لعمليات القراءة مقابل التعديل ويفرض عزل البيانات بين الجهات.
    /// </summary>
    // ──────────────────────────────────────────────────────────────────────────
    // SECURITY & ACCESS CONTROL HELPER (غيث: صلاحية مطلقة | أسيزا: رقابي فقط View-Only)
    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// EN: Evaluates user permissions using SystemRoles for read vs. write operations and enforces tenant isolation.
    /// AR: يفحص صلاحيات المستخدم باستخدام ثوابت النظام لعمليات القراءة مقابل التعديل ويفرض عزل البيانات بين الجهات.
    /// </summary>
    private static (bool IsAllowed, int StatusCode, string ErrorEn, string ErrorAr) AuthorizeUser(
        User? user,
        Project project,
        bool isWriteOperation,
        int? workerAssociationId = null)
    {
        if (user is null || !user.IsActive)
        {
            return (false, 401, "Unauthorized user.", "المستخدم غير مصرح له أو غير فعال.");
        }

        // 1. Ghaith / SuperAdmin (BaytAlUrdon = 1): Absolute full access (Read & Write across the entire system)
        // ١. غيث / بيت الأردن (صلاحية 1): صلاحية مطلقة للقراءة والتعديل على كامل النظام
        if (user.RoleId == SystemRoles.BaytAlUrdon)
        {
            return (true, 200, string.Empty, string.Empty);
        }

        // 2. Asiza (Oversight Role = 2): Strictly View-Only (GET allowed, any Write is 403 Forbidden)
        // ٢. أسيزا (دور رقابي صلاحية 2): مسموح لها العرض فقط، ويُمنع منعاً باتاً أي إضافة أو تعديل أو حذف
        if (user.RoleId == SystemRoles.Aseza)
        {
            if (isWriteOperation)
            {
                return (false, 403,
                    "Access Denied: Oversight role (Aseza) has view-only permissions and cannot modify project workers.",
                    "غير مصرح: الدور الرقابي (أسيزا) يمتلك صلاحية العرض فقط ولا يمكنه إضافة أو تعديل عمال المشروع.");
            }

            return (true, 200, string.Empty, string.Empty);
        }

        // 3. Production Company Users: Scoped strictly to their own company's projects
        // ٣. مستخدمو شركات الإنتاج: صلاحيتهم محصورة فقط بالمشاريع التابعة لشركة الإنتاج الخاصة بهم
        if (user.RoleId == SystemRoles.ProductionHouse || user.RoleId == SystemRoles.Producer || user.RoleId == SystemRoles.LocationManager)
        {
            if (user.EntityId != project.ProductionCompanyId)
            {
                return (false, 403,
                    "Access Denied: Project belongs to another production company.",
                    "غير مصرح: هذا المشروع يتبع لشركة إنتاج أخرى.");
            }

            return (true, 200, string.Empty, string.Empty);
        }

        // 4. Association Users: Scoped to their own association's workers
        // ٤. مستخدمو الجمعيات: تستعرض أو تدير فقط العمال التابعين لجمعيتها
        if (user.RoleId == SystemRoles.Association || user.RoleId == SystemRoles.Admin_Association || user.RoleId == SystemRoles.Association_Representative)
        {
            if (workerAssociationId.HasValue && user.EntityId != workerAssociationId.Value)
            {
                return (false, 403,
                    "Access Denied: Worker belongs to another association.",
                    "غير مصرح: هذا العامل يتبع لجمعية أخرى.");
            }

            return (true, 200, string.Empty, string.Empty);
        }

        return (false, 403, "Forbidden: Insufficient permissions.", "غير مصرح لك بتنفيذ هذه العملية.");
    }

    public async Task<ApiResponse<List<ProjectWorkerDto>>> GetByProjectIdAsync(
        int projectId,
        string? searchByWorkerName,
        string? searchByWorkerCivilianId,
        bool? isWorkerActive,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (projectId <= 0)
        {
            return ApiResponse<List<ProjectWorkerDto>>.FailureResponse(
                "Invalid project ID.", "معرف المشروع غير صالح.");
        }

        var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            var notFound = ApiResponse<List<ProjectWorkerDto>>.FailureResponse(
                "Project not found.", "المشروع غير موجود.");
            notFound.StatusCode = 404;
            return notFound;
        }

        var auth = AuthorizeUser(currentUser, project, isWriteOperation: false);
        if (!auth.IsAllowed)
        {
            var forbidden = ApiResponse<List<ProjectWorkerDto>>.FailureResponse(auth.ErrorEn, auth.ErrorAr);
            forbidden.StatusCode = auth.StatusCode;
            return forbidden;
        }

        var items = await _unitOfWork.ProjectWorkers.GetByProjectIdAsync(
            projectId, searchByWorkerName, searchByWorkerCivilianId, isWorkerActive, cancellationToken);
        // EN: Scope results to the user's association if the caller is an Association user.
        // AR: حصر النتائج بعمال الجمعية الخاصة بالمستخدم إذا كان حساب المستخدم تابعاً لجمعية.
        if (currentUser!.EntityType == EntityType.Association && currentUser.EntityId > 0)
        {
            items = items.Where(pw => pw.Worker != null && pw.Worker.AssociationId == currentUser.EntityId).ToList();
        }

        var dtos = items.Select(MapToDto).ToList();
        return ApiResponse<List<ProjectWorkerDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<ProjectWorkerDto>> GetByIdAsync(
        int id, int currentUserId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.ProjectWorkers.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            var notFound = ApiResponse<ProjectWorkerDto>.FailureResponse(
                "Project worker assignment not found.", "سجل تعيين العامل في المشروع غير موجود.");
            notFound.StatusCode = 404;
            return notFound;
        }

        var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        var auth = AuthorizeUser(currentUser, item.Project, isWriteOperation: false, workerAssociationId: item.Worker?.AssociationId);
        if (!auth.IsAllowed)
        {
            var forbidden = ApiResponse<ProjectWorkerDto>.FailureResponse(auth.ErrorEn, auth.ErrorAr);
            forbidden.StatusCode = auth.StatusCode;
            return forbidden;
        }

        return ApiResponse<ProjectWorkerDto>.SuccessResponse(MapToDto(item));
    }

    public async Task<ApiResponse<ProjectWorkerDto>> CreateAsync(
        CreateProjectWorkerDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (dto.ProjectId <= 0 || dto.WorkerId <= 0)
        {
            return ApiResponse<ProjectWorkerDto>.FailureResponse(
                "ProjectId and WorkerId are required.", "معرف المشروع ومعرف العامل مطلوبان.");
        }

        if (dto.DailyWageAmount < 0)
        {
            return ApiResponse<ProjectWorkerDto>.FailureResponse(
                "Daily wage amount cannot be negative.", "لا يمكن أن تكون قيمة الأجر اليومي سالبة.");
        }

        var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        var project = await _unitOfWork.Projects.GetByIdAsync(dto.ProjectId, cancellationToken);
        if (project is null)
        {
            var notFound = ApiResponse<ProjectWorkerDto>.FailureResponse(
                "Project not found.", "المشروع غير موجود.");
            notFound.StatusCode = 404;
            return notFound;
        }

        var worker = await _unitOfWork.Workers.GetByIdAsync(dto.WorkerId, cancellationToken);
        if (worker is null)
        {
            var notFound = ApiResponse<ProjectWorkerDto>.FailureResponse(
                "Worker not found.", "العامل غير موجود.");
            notFound.StatusCode = 404;
            return notFound;
        }

        var auth = AuthorizeUser(currentUser, project, isWriteOperation: true, workerAssociationId: worker.AssociationId);
        if (!auth.IsAllowed)
        {
            var forbidden = ApiResponse<ProjectWorkerDto>.FailureResponse(auth.ErrorEn, auth.ErrorAr);
            forbidden.StatusCode = auth.StatusCode;
            return forbidden;
        }

        bool alreadyAssigned = await _unitOfWork.ProjectWorkers.ExistsAsync(dto.ProjectId, dto.WorkerId, cancellationToken);
        if (alreadyAssigned)
        {
            return ApiResponse<ProjectWorkerDto>.FailureResponse(
                "This worker is already assigned to the project.",
                "هذا العامل معين بالفعل في هذا المشروع.");
        }

        var entity = new ProjectWorker
        {
            ProjectId = dto.ProjectId,
            WorkerId = dto.WorkerId,
            DailyWageAmount = dto.DailyWageAmount,
            IsWorkerActive = true,
            IsActive = true,
            CreatedBy = currentUserId
        };

        await _unitOfWork.ProjectWorkers.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _unitOfWork.ProjectWorkers.GetByIdAsync(entity.Id, cancellationToken);
        return ApiResponse<ProjectWorkerDto>.SuccessResponse(
            MapToDto(created!),
            "Worker assigned to project successfully.",
            "تم تعيين العامل في المشروع بنجاح.",
            201);
    }

    public async Task<ApiResponse<ProjectWorkerDto>> UpdateAsync(
        int id, UpdateProjectWorkerDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (dto.DailyWageAmount < 0)
        {
            return ApiResponse<ProjectWorkerDto>.FailureResponse(
                "Daily wage amount cannot be negative.", "لا يمكن أن تكون قيمة الأجر اليومي سالبة.");
        }

        var entity = await _unitOfWork.ProjectWorkers.GetByIdForUpdateAsync(id, cancellationToken);
        if (entity is null)
        {
            var notFound = ApiResponse<ProjectWorkerDto>.FailureResponse(
                "Project worker assignment not found.", "سجل تعيين العامل في المشروع غير موجود.");
            notFound.StatusCode = 404;
            return notFound;
        }

        var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        var auth = AuthorizeUser(currentUser, entity.Project, isWriteOperation: true, workerAssociationId: entity.Worker?.AssociationId);
        if (!auth.IsAllowed)
        {
            var forbidden = ApiResponse<ProjectWorkerDto>.FailureResponse(auth.ErrorEn, auth.ErrorAr);
            forbidden.StatusCode = auth.StatusCode;
            return forbidden;
        }

        if (dto.WorkerId > 0 && dto.WorkerId != entity.WorkerId)
        {
            var newWorker = await _unitOfWork.Workers.GetByIdAsync(dto.WorkerId, cancellationToken);
            if (newWorker is null)
            {
                var notFound = ApiResponse<ProjectWorkerDto>.FailureResponse(
                    "Worker not found.", "العامل غير موجود.");
                notFound.StatusCode = 404;
                return notFound;
            }

            bool duplicate = await _unitOfWork.ProjectWorkers.ExistsAsync(entity.ProjectId, dto.WorkerId, cancellationToken);
            if (duplicate)
            {
                return ApiResponse<ProjectWorkerDto>.FailureResponse(
                    "The selected worker is already assigned to this project.",
                    "العامل المختار معين مسبقاً في هذا المشروع.");
            }

            entity.WorkerId = dto.WorkerId;
        }

        entity.DailyWageAmount = dto.DailyWageAmount;
        entity.IsWorkerActive = dto.IsWorkerActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _unitOfWork.ProjectWorkers.GetByIdAsync(entity.Id, cancellationToken);
        return ApiResponse<ProjectWorkerDto>.SuccessResponse(
            MapToDto(updated!),
            "Project worker updated successfully.",
            "تم تحديث بيانات العامل في المشروع بنجاح.");
    }

    public async Task<ApiResponse<ProjectWorkerDto>> ActivateAsync(
        int id, int currentUserId, CancellationToken cancellationToken = default)
    {
        return await SetWorkerActiveStatusAsync(id, isActive: true, currentUserId, cancellationToken);
    }

    public async Task<ApiResponse<ProjectWorkerDto>> DeactivateAsync(
        int id, int currentUserId, CancellationToken cancellationToken = default)
    {
        return await SetWorkerActiveStatusAsync(id, isActive: false, currentUserId, cancellationToken);
    }

    private async Task<ApiResponse<ProjectWorkerDto>> SetWorkerActiveStatusAsync(
        int id, bool isActive, int currentUserId, CancellationToken cancellationToken)
    {
        var entity = await _unitOfWork.ProjectWorkers.GetByIdForUpdateAsync(id, cancellationToken);
        if (entity is null)
        {
            var notFound = ApiResponse<ProjectWorkerDto>.FailureResponse(
                "Project worker assignment not found.", "سجل تعيين العامل في المشروع غير موجود.");
            notFound.StatusCode = 404;
            return notFound;
        }

        var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        var auth = AuthorizeUser(currentUser, entity.Project, isWriteOperation: true, workerAssociationId: entity.Worker?.AssociationId);
        if (!auth.IsAllowed)
        {
            var forbidden = ApiResponse<ProjectWorkerDto>.FailureResponse(auth.ErrorEn, auth.ErrorAr);
            forbidden.StatusCode = auth.StatusCode;
            return forbidden;
        }

        entity.IsWorkerActive = isActive;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _unitOfWork.ProjectWorkers.GetByIdAsync(entity.Id, cancellationToken);
        string msgEn = isActive ? "Worker activated on project successfully." : "Worker deactivated on project successfully.";
        string msgAr = isActive ? "تم تفعيل العامل في المشروع بنجاح." : "تم إيقاف تفعيل العامل في المشروع بنجاح.";

        return ApiResponse<ProjectWorkerDto>.SuccessResponse(MapToDto(updated!), msgEn, msgAr);
    }

    private static ProjectWorkerDto MapToDto(ProjectWorker entity)
    {
        var fullName = entity.Worker is null
            ? string.Empty
            : string.Join(" ", new[] { entity.Worker.FirstName, entity.Worker.MiddleName, entity.Worker.LastName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        var civilId = entity.Worker?.CivilId ?? string.Empty;

        return new ProjectWorkerDto
        {
            Id = entity.Id,
            ProjectId = entity.ProjectId,
            ProjectNameAr = entity.Project?.ProjectNameAr ?? string.Empty,
            ProjectNameEn = entity.Project?.ProjectNameEn ?? string.Empty,
            WorkerId = entity.WorkerId,
            WorkerName = fullName,
            WorkerCivilainId = civilId,
            WorkerCivilId = civilId,
            WorkerPhoneNumber = entity.Worker?.PhoneNumber,
            DailyWageAmount = entity.DailyWageAmount,
            IsWorkerActive = entity.IsWorkerActive,
            CreatedAt = entity.CreatedAt
        };
    }
}