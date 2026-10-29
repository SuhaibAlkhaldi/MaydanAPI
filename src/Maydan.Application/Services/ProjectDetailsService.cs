using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.ProjectDetails;
using Maydan.Application.Interfaces;
using Maydan.Domain.Common;
using Maydan.Domain.Constants;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

public class ProjectDetailsService : IProjectDetailsService
{
    // Jordan is permanently UTC+3.
    private static readonly TimeSpan LocalOffset = TimeSpan.FromHours(3);

    private readonly IUnitOfWork _uow;
    private readonly IWorkerProjectLinkRepository _linkRepo;

    public ProjectDetailsService(IUnitOfWork uow, IWorkerProjectLinkRepository linkRepo)
    {
        _uow = uow;
        _linkRepo = linkRepo;
    }

    
    //  GET full details
    
    public async Task<ApiResponse<ProjectDetailsDto>> GetProjectFullDetailsAsync(int currentUserId, int projectId, CancellationToken ct = default)
    {
        var (project, error) = await LoadAuthorizedProjectAsync<ProjectDetailsDto>(currentUserId, projectId, ct);
        if (error is not null) return error;

        var today = (DateTime.UtcNow + LocalOffset).Date;

        var supervisors = await _uow.AssociationProjectSupervisors.GetByProjectIdAsync(projectId, ct);
        var workerLinks = await _linkRepo.ListByProjectAsync(projectId, ct);
        var allServiceRequests = await _uow.ServiceRequests.GetAllAsync(ct);
        var projectServiceRequests = allServiceRequests.Where(sr => sr.ProjectId == projectId).ToList();

        var dto = new ProjectDetailsDto
        {
            Project = new ProjectDetailsInfoDto
            {
                Id = project!.Id,
                ProjectNameEn = project.ProjectNameEn,
                ProjectNameAr = project.ProjectNameAr,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                ProjectType = project.ProjectType?.NameEn,
                WorkPermitImagePath = project.WorkPermitImagePath,
                ProductionCompanyId = project.ProductionCompanyId,
                ProductionCompanyName = project.ProductionCompany?.EnglishName
            },
            Producer = ToPerson(project.Producer),
            LocationManager = ToPerson(project.LocationManager),
            AssociationSupervisors = supervisors?.Select(s => new ProjectDetailsSupervisorDto
            {
                Id = s.Id,
                UserId = s.UserId,
                FullNameEn = s.User != null ? $"{s.User.FirstNameEn} {s.User.LastNameEn}" : null,
                FullNameAr = s.User != null ? $"{s.User.FirstNameAr} {s.User.LastNameAr}" : null,
                Email = s.User?.Email,
                PhoneNumber = s.User?.PhoneNumber
            }).ToList() ?? new List<ProjectDetailsSupervisorDto>(),
            Workers = workerLinks.Select(wl => ToWorkerDto(wl, today)).ToList(),
            ServiceRequests = projectServiceRequests.Select(sr => new ProjectDetailsServiceRequestDto
            {
                Id = sr.Id,
                ServiceId = sr.ServiceId,
                ServiceNameEn = sr.Service?.NameEn,
                ServiceNameAr = sr.Service?.NameAr,
                AssociationId = sr.AssociationId,
                AssociationNameEn = sr.Association?.EnglishName,
                AssociationNameAr=sr.Association?.ArabicName,
                StartDate = sr.StartDate,
                EndDate = sr.EndDate,
                ShiftsCount = sr.ShiftsCount,
                RequestedWorkersCount = sr.RequestedWorkersCount,
                SelectedWorkersCount = sr.SelectedWorkersCount,
                AttendanceFrequency = sr.AttendanceFrequency,
                UnitPriceSnapshot = sr.UnitPriceSnapshot,
                ExpectedTotalAmount = sr.ExpectedTotalAmount,
                Status = LookupConstants.ServiceRequestStatusCode(sr.StatusId),
                AdditionalRequirements = sr.AdditionalRequirements,
                RejectionReason = sr.RejectionReason,
                AssignedWorkers = sr.ServiceRequestWorkers?.Select(srw => new ProjectDetailsAssignedWorkerDto
                {
                    WorkerId = srw.WorkerId,
                    WorkerName = srw.Worker != null ? $"{srw.Worker.FirstName} {srw.Worker.LastName}" : null
                }).ToList() ?? new List<ProjectDetailsAssignedWorkerDto>()
            }).ToList()
        };

        return ApiResponse<ProjectDetailsDto>.SuccessResponse(dto, "Project details fetched successfully.", "تم جلب تفاصيل المشروع بنجاح.");
    }

    //  GET workers only
    public async Task<ApiResponse<List<ProjectDetailsSupervisorDto>>> GetProjectSupervisorsAsync(int currentUserId, int projectId, CancellationToken ct = default)
    {
        var (_, error) = await LoadAuthorizedProjectAsync<List<ProjectDetailsSupervisorDto>>(currentUserId, projectId, ct);
        if (error is not null) return error;

        var supervisors = await _uow.AssociationProjectSupervisors.GetByProjectIdAsync(projectId, ct);

        var dtos = supervisors?.Select(s => new ProjectDetailsSupervisorDto
        {
            Id = s.Id,
            UserId = s.UserId,
            FullNameEn = s.User != null ? $"{s.User.FirstNameEn} {s.User.LastNameEn}" : null,
            FullNameAr = s.User != null ? $"{s.User.FirstNameAr} {s.User.LastNameAr}" : null,
            Email = s.User?.Email,
            PhoneNumber = s.User?.PhoneNumber
        }).ToList() ?? new List<ProjectDetailsSupervisorDto>();

        return ApiResponse<List<ProjectDetailsSupervisorDto>>.SuccessResponse(
            dtos, "Project supervisors fetched successfully.", "تم جلب مشرفي المشروع بنجاح.");
    }

    public async Task<ApiResponse<List<ProjectDetailsWorkerDto>>> GetProjectWorkersAsync(int currentUserId, int projectId, CancellationToken ct = default)
    {
        var (_, error) = await LoadAuthorizedProjectAsync<List<ProjectDetailsWorkerDto>>(currentUserId, projectId, ct);
        if (error is not null) return error;

        var today = (DateTime.UtcNow + LocalOffset).Date;
        var workerLinks = await _linkRepo.ListByProjectAsync(projectId, ct);

        return ApiResponse<List<ProjectDetailsWorkerDto>>.SuccessResponse(
            workerLinks.Select(wl => ToWorkerDto(wl, today)).ToList(),
            "Project workers fetched successfully.", "تم جلب عمال المشروع بنجاح.");
    }

   
    //  Access rule
    
    // Who may see project details:
    //  1) the Bayt Al Urdon admin (role Bayt-AlUrdon, e.g. Ghaith), for any project;
    //  2) the Production House admin (role ProductionHouse), ONLY for projects of his own company.
    // Everyone else (Aseza, producer, location manager, association users, workers...) is refused.
    private async Task<(Project? Project, ApiResponse<T>? Error)> LoadAuthorizedProjectAsync<T>(int currentUserId, int projectId, CancellationToken ct)
    {
        var user = await _uow.Users.GetWithPermissionsAsync(currentUserId, ct);
        if (user is null || !user.IsActive)
            return (null, Fail<T>("Current user was not found or is inactive.", "لم يتم العثور على المستخدم الحالي أو أن حسابه غير نشط.", 401));

        var isBaytAlUrdonAdmin = user.RoleId == SystemRoles.BaytAlUrdon && user.EntityType == EntityType.BaytAlUrdon;
        var isProductionHouseAdmin = user.RoleId == SystemRoles.ProductionHouse && user.EntityType == EntityType.ProductionCompany;

        if (!isBaytAlUrdonAdmin && !isProductionHouseAdmin)
            return (null, Forbidden<T>());

        var project = await _uow.Projects.GetByIdAsync(projectId, ct);
        if (project is null)
            return (null, Fail<T>("Project was not found.", "المشروع غير موجود.", 404));

        if (isProductionHouseAdmin && user.EntityId != project.ProductionCompanyId)
            return (null, Forbidden<T>());

        return (project, null);
    }

    private static ApiResponse<T> Forbidden<T>() =>
        Fail<T>("Only the Bayt Al Urdon admin or the admin of the project's Production House can view project details.",
                "فقط أدمن بيت الأردن أو أدمن شركة الإنتاج صاحبة المشروع يمكنه عرض تفاصيل المشروع.", 403);

    private static ApiResponse<T> Fail<T>(string en, string ar, int status)
    {
        var r = ApiResponse<T>.FailureResponse(en, ar);
        r.StatusCode = status;
        return r;
    }

   
    //  Mapping
    
    private static ProjectDetailsPersonDto? ToPerson(User? u) => u is null ? null : new ProjectDetailsPersonDto
    {
        UserId = u.UserId,
        FullNameEn = $"{u.FirstNameEn} {u.LastNameEn}",
        FullNameAr = $"{u.FirstNameAr} {u.LastNameAr}",
        Email = u.Email,
        PhoneNumber = u.PhoneNumber
    };

    private static ProjectDetailsWorkerDto ToWorkerDto(WorkerProjectLink wl, DateTime today) => new()
    {
        WorkerProjectLinkId = wl.Id,
        WorkerId = wl.WorkerId,
        WorkerFullName = wl.Worker != null ? $"{wl.Worker.FirstName} {wl.Worker.LastName}" : null,
        CivilId = wl.Worker?.CivilId,
        PhoneNumber = wl.Worker?.PhoneNumber,
        IsWorkerActive = wl.IsWorkerActive,
        DailyWageAmount = wl.DailyWageAmount,
        ServiceRequestId = wl.ServiceRequestId,
        ServiceNameEn = wl.ServiceRequest?.Service?.NameEn,
        ServiceNameAr = wl.ServiceRequest?.Service?.NameAr,
        ServicePrice = wl.ServiceRequest?.Service?.Price,
        AssociationName = wl.ServiceRequest?.Association?.EnglishName,
        CreatedDate = wl.CreatedDate,
        ServiceStartDate = wl.ServiceRequest?.StartDate,
        ServiceEndDate = wl.ServiceRequest?.EndDate,
        ServiceRequestStatus = wl.ServiceRequest is null ? null : LookupConstants.ServiceRequestStatusCode(wl.ServiceRequest.StatusId),
        IsCurrent = wl.IsWorkerActive
                    && wl.ServiceRequest != null
                    && wl.ServiceRequest.StatusId == LookupConstants.ServiceRequestStatus.InProgress
                    && wl.ServiceRequest.EndDate >= today
    };
}
