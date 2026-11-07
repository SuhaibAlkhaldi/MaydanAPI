using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Projects;
using Maydan.Application.Interfaces;
using Maydan.Domain.Constants;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

/// <summary>
/// EN: Implements business logic and strict authorization for ProjectAssociations and their geographical locations.
/// AR: ينفذ منطق الأعمال والتحقق الصارم من الصلاحيات لجمعيات المشروع ومواقعها الجغرافية.
/// </summary>
public class ProjectAssociationService : IProjectAssociationService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProjectAssociationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SECURITY & ACCESS CONTROL HELPER (غيث: صلاحية مطلقة | أسيزا: رقابي فقط View-Only)
    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// EN: Evaluates user permissions for read vs. write operations and enforces tenant isolation.
    /// AR: يفحص صلاحيات المستخدم لعمليات القراءة مقابل التعديل ويفرض عزل البيانات بين الجهات.
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
        int? targetAssociationId = null)
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
                    "Access Denied: Oversight role (Aseza) has view-only permissions and cannot modify data.",
                    "غير مصرح: الدور الرقابي (أسيزا) يمتلك صلاحية العرض فقط ولا يمكنه الإضافة أو التعديل أو الحذف.");
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
                    "Access Denied: You do not have permission to access projects outside your production company.",
                    "غير مصرح: لا يمكنك الوصول إلى مشاريع لا تتبع لشركة الإنتاج الخاصة بك.");
            }

            return (true, 200, string.Empty, string.Empty);
        }

        // 4. Association Users: View-only access scoped strictly to their own association assignments
        // ٤. مستخدمو الجمعيات: صلاحية عرض فقط ومحصورة بالسجلات المرتبطة بجمعيتهم
        if (user.RoleId == SystemRoles.Association || user.RoleId == SystemRoles.Admin_Association || user.RoleId == SystemRoles.Association_Representative)
        {
            if (isWriteOperation)
            {
                return (false, 403,
                    "Access Denied: Association users cannot modify project assignments.",
                    "غير مصرح: لا يمكن لمستخدمي الجمعية إضافة أو تعديل بيانات المشروع.");
            }

            if (targetAssociationId.HasValue && user.EntityId != targetAssociationId.Value)
            {
                return (false, 403,
                    "Access Denied: You can only view records linked to your association.",
                    "غير مصرح: يمكنك فقط عرض السجلات المرتبطة بجمعيتك.");
            }

            return (true, 200, string.Empty, string.Empty);
        }

        return (false, 403, "Forbidden: Insufficient permissions.", "غير مصرح لك بتنفيذ هذه العملية.");
    }

    public async Task<ApiResponse<List<ProjectAssociationDto>>> GetByProjectIdAsync(
        int projectId, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (projectId <= 0)
        {
            return ApiResponse<List<ProjectAssociationDto>>.FailureResponse(
                "Invalid project ID.", "معرف المشروع غير صالح.");
        }

        var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId, cancellationToken);

        if (project is null)
        {
            var notFound = ApiResponse<List<ProjectAssociationDto>>.FailureResponse(
                "Project not found.", "المشروع غير موجود.");
            notFound.StatusCode = 404;
            return notFound;
        }

        var auth = AuthorizeUser(currentUser, project, isWriteOperation: false);
        if (!auth.IsAllowed)
        {
            var forbidden = ApiResponse<List<ProjectAssociationDto>>.FailureResponse(auth.ErrorEn, auth.ErrorAr);
            forbidden.StatusCode = auth.StatusCode;
            return forbidden;
        }

        var items = await _unitOfWork.ProjectAssociations.GetByProjectIdAsync(projectId, cancellationToken);

        // EN: If the caller is an Association user, filter results strictly to their AssociationId.
        // AR: إذا كان المستخدم تابعاً لجمعية، يتم فلترة النتائج لتقتصر على جمعيته فقط.
        // EN: If the caller is an Association user, filter results strictly to their AssociationId.
        // AR: إذا كان المستخدم تابعاً لجمعية، يتم فلترة النتائج لتقتصر على جمعيته فقط.
        if (currentUser!.EntityType == EntityType.Association && currentUser.EntityId > 0)
        {
            items = items.Where(pa => pa.AssociationId == currentUser.EntityId).ToList();
        }

        var dtos = items.Select(MapToDto).ToList();
        return ApiResponse<List<ProjectAssociationDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<ProjectAssociationDto>> GetByIdAsync(
        int id, int currentUserId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.ProjectAssociations.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            var notFound = ApiResponse<ProjectAssociationDto>.FailureResponse(
                "Project association record not found.", "سجل ربط الجمعية بالمشروع غير موجود.");
            notFound.StatusCode = 404;
            return notFound;
        }

        var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        var auth = AuthorizeUser(currentUser, item.Project, isWriteOperation: false, targetAssociationId: item.AssociationId);
        if (!auth.IsAllowed)
        {
            var forbidden = ApiResponse<ProjectAssociationDto>.FailureResponse(auth.ErrorEn, auth.ErrorAr);
            forbidden.StatusCode = auth.StatusCode;
            return forbidden;
        }

        return ApiResponse<ProjectAssociationDto>.SuccessResponse(MapToDto(item));
    }

    public async Task<ApiResponse<ProjectAssociationDto>> CreateAsync(
        CreateProjectAssociationDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        // EN: Validate input parameters.
        // AR: التحقق من صحة المدخلات الأساسية.
        if (dto.ProjectId <= 0 || dto.AssociationId <= 0)
        {
            return ApiResponse<ProjectAssociationDto>.FailureResponse(
                "ProjectId and AssociationId are required.", "معرف المشروع ومعرف الجمعية مطلوبان.");
        }

        var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        var project = await _unitOfWork.Projects.GetByIdAsync(dto.ProjectId, cancellationToken);
        if (project is null)
        {
            var notFound = ApiResponse<ProjectAssociationDto>.FailureResponse(
                "Project not found.", "المشروع غير موجود.");
            notFound.StatusCode = 404;
            return notFound;
        }

        // EN: Enforce security check (Blocks Asiza and unauthorized tenants).
        // AR: فحص الصلاحيات الأمني (يمنع دور أسيزا الرقابي وأي جهة غير مخولة من الإضافة).
        var auth = AuthorizeUser(currentUser, project, isWriteOperation: true);
        if (!auth.IsAllowed)
        {
            var forbidden = ApiResponse<ProjectAssociationDto>.FailureResponse(auth.ErrorEn, auth.ErrorAr);
            forbidden.StatusCode = auth.StatusCode;
            return forbidden;
        }

        var association = await _unitOfWork.Associations.GetByIdAsync(dto.AssociationId, cancellationToken);
        if (association is null)
        {
            var notFound = ApiResponse<ProjectAssociationDto>.FailureResponse(
                "Association not found.", "الجمعية غير موجودة.");
            notFound.StatusCode = 404;
            return notFound;
        }

        // EN: Prevent duplicate link between the same project and association.
        // AR: منع تكرار ربط نفس الجمعية بنفس المشروع.
        bool alreadyExists = await _unitOfWork.ProjectAssociations.ExistsAsync(dto.ProjectId, dto.AssociationId, null, cancellationToken);
        if (alreadyExists)
        {
            return ApiResponse<ProjectAssociationDto>.FailureResponse(
                "This association is already linked to the project.",
                "هذه الجمعية مرتبطة بالفعل بهذا المشروع.");
        }

        var entity = new ProjectAssociation
        {
            ProjectId = dto.ProjectId,
            AssociationId = dto.AssociationId,
            CreatedBy = currentUserId,
            IsActive = true,
            Locations = (dto.Locations ?? new List<CreateUpdateProjectAssociationLocationDto>())
                .Where(l => !string.IsNullOrWhiteSpace(l.Name))
                .Select(l => new ProjectAssociationLocation
                {
                    Name = l.Name.Trim(),
                    Description = l.Description?.Trim(),
                    LocationOnGoogleMaps = l.LocationOnGoogleMaps?.Trim(),
                    Latitude = l.Latitude?.Trim(),
                    Longitude = l.Longitude?.Trim(),
                    CreatedBy = currentUserId,
                    IsActive = true
                }).ToList()
        };

        await _unitOfWork.ProjectAssociations.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _unitOfWork.ProjectAssociations.GetByIdAsync(entity.Id, cancellationToken);
        return ApiResponse<ProjectAssociationDto>.SuccessResponse(
            MapToDto(created!),
            "Project association created successfully.",
            "تم ربط الجمعية والمواقع بالمشروع بنجاح.",
            201);
    }

    public async Task<ApiResponse<ProjectAssociationDto>> UpdateAsync(
        int id, UpdateProjectAssociationDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (dto.AssociationId <= 0)
        {
            return ApiResponse<ProjectAssociationDto>.FailureResponse(
                "Valid AssociationId is required.", "معرف الجمعية مطلوب.");
        }

        var entity = await _unitOfWork.ProjectAssociations.GetByIdForUpdateAsync(id, cancellationToken);
        if (entity is null)
        {
            var notFound = ApiResponse<ProjectAssociationDto>.FailureResponse(
                "Project association record not found.", "سجل ربط الجمعية بالمشروع غير موجود.");
            notFound.StatusCode = 404;
            return notFound;
        }

        var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        var auth = AuthorizeUser(currentUser, entity.Project, isWriteOperation: true);
        if (!auth.IsAllowed)
        {
            var forbidden = ApiResponse<ProjectAssociationDto>.FailureResponse(auth.ErrorEn, auth.ErrorAr);
            forbidden.StatusCode = auth.StatusCode;
            return forbidden;
        }

        var association = await _unitOfWork.Associations.GetByIdAsync(dto.AssociationId, cancellationToken);
        if (association is null)
        {
            var notFound = ApiResponse<ProjectAssociationDto>.FailureResponse(
                "Association not found.", "الجمعية غير موجودة.");
            notFound.StatusCode = 404;
            return notFound;
        }

        bool duplicateExists = await _unitOfWork.ProjectAssociations.ExistsAsync(
            entity.ProjectId, dto.AssociationId, excludeId: id, cancellationToken);
        if (duplicateExists)
        {
            return ApiResponse<ProjectAssociationDto>.FailureResponse(
                "Another record already links this association to the project.",
                "يوجد سجل آخر يربط هذه الجمعية بنفس المشروع مسبقاً.");
        }

        entity.AssociationId = dto.AssociationId;

        // EN: Soft-delete old locations (via MaydanDbContext SaveChanges interceptor) and replace with the updated list.
        // AR: حذف المواقع السابقة منطقياً واستبدالها بقائمة المواقع المحدثة.
        foreach (var existingLocation in entity.Locations.ToList())
        {
            existingLocation.IsDeleted = true;
            existingLocation.DeletedAt = DateTime.UtcNow;
        }

        var newLocations = (dto.Locations ?? new List<CreateUpdateProjectAssociationLocationDto>())
            .Where(l => !string.IsNullOrWhiteSpace(l.Name))
            .Select(l => new ProjectAssociationLocation
            {
                ProjectAssociationId = entity.Id,
                Name = l.Name.Trim(),
                Description = l.Description?.Trim(),
                LocationOnGoogleMaps = l.LocationOnGoogleMaps?.Trim(),
                Latitude = l.Latitude?.Trim(),
                Longitude = l.Longitude?.Trim(),
                CreatedBy = currentUserId,
                IsActive = true
            }).ToList();

        foreach (var loc in newLocations)
        {
            entity.Locations.Add(loc);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _unitOfWork.ProjectAssociations.GetByIdAsync(entity.Id, cancellationToken);
        return ApiResponse<ProjectAssociationDto>.SuccessResponse(
            MapToDto(updated!),
            "Project association updated successfully.",
            "تم تحديث بيانات الجمعية والمواقع بنجاح.");
    }

    public async Task<ApiResponse<object?>> DeleteAsync(
        int id, int currentUserId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.ProjectAssociations.GetByIdForUpdateAsync(id, cancellationToken);
        if (entity is null)
        {
            var notFound = ApiResponse<object?>.FailureResponse(
                "Project association record not found.", "سجل ربط الجمعية بالمشروع غير موجود.");
            notFound.StatusCode = 404;
            return notFound;
        }

        var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);
        var auth = AuthorizeUser(currentUser, entity.Project, isWriteOperation: true);
        if (!auth.IsAllowed)
        {
            var forbidden = ApiResponse<object?>.FailureResponse(auth.ErrorEn, auth.ErrorAr);
            forbidden.StatusCode = auth.StatusCode;
            return forbidden;
        }

        // EN: Soft-delete nested locations as well as the parent assignment.
        // AR: حذف المواقع الفرعية منطقياً مع حذف سجل الربط الأب.
        foreach (var loc in entity.Locations)
        {
            loc.IsDeleted = true;
            loc.DeletedAt = DateTime.UtcNow;
        }

        _unitOfWork.ProjectAssociations.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object?>.SuccessResponse(
            null,
            "Project association deleted successfully.",
            "تم حذف ارتباط الجمعية بالمشروع بنجاح.");
    }

    private static ProjectAssociationDto MapToDto(ProjectAssociation entity) => new()
    {
        Id = entity.Id,
        ProjectId = entity.ProjectId,
        ProjectNameAr = entity.Project?.ProjectNameAr ?? string.Empty,
        ProjectNameEn = entity.Project?.ProjectNameEn ?? string.Empty,
        AssociationId = entity.AssociationId,
        AssociationNameAr = entity.Association?.ArabicName ?? string.Empty,
        AssociationNameEn = entity.Association?.EnglishName ?? string.Empty,
        CreatedAt = entity.CreatedAt,
        Locations = entity.Locations
            .Where(l => !l.IsDeleted)
            .Select(l => new ProjectAssociationLocationDto
            {
                Id = l.Id,
                ProjectAssociationId = l.ProjectAssociationId,
                Name = l.Name,
                Description = l.Description,
                LocationOnGoogleMaps = l.LocationOnGoogleMaps,
                Latitude = l.Latitude,
                Longitude = l.Longitude
            }).ToList()
    };
}