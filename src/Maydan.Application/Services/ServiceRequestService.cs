using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.ServiceRequests;
using Maydan.Application.Exceptions;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;
using System.Linq;

namespace Maydan.Application.Services;

public class ServiceRequestService : IServiceRequestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;

    //private const int RequestService = 15;
    //private const int ViewServiceRequests = 16;
    //private const int ManageServiceRequestsPermissionId = 17;
    private const string RequestServicePermission = "Request Service";
    private const string ViewServiceRequestsPermission = "View Service Requests";
    private const string ManageServiceRequestsPermission = "Manage Service Requests";
    //PermissionNameEn
    public ServiceRequestService(IUnitOfWork unitOfWork, IEmailSender emailSender)
    {
        _unitOfWork = unitOfWork;
        _emailSender = emailSender;
    }
    private async Task<(User? CurrentUser, ApiResponse<T>? Error)> GetCurrentUserAsync<T>(int currentUserId, CancellationToken cancellationToken)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken);
        if (currentUser is null) { return (null, ApiResponse<T>.FailureResponse("لم يتم العثور على المستخدم الحالي.", "Current user was not found.")); }
        if (!currentUser.IsActive)
        {
            return (null, ApiResponse<T>.FailureResponse("حساب المستخدم الحالي غير نشط.", "Current user is inactive."));
        }
        return (currentUser, null);
    }
    private static ApiResponse<T>? EnsureCanRequestService<T>(User currentUser)
    {
        var permissions = GetEffectivePermissions(currentUser);

        if (!permissions.Contains(RequestServicePermission) &&
            !permissions.Contains(ManageServiceRequestsPermission))
        {
            return ApiResponse<T>.FailureResponse(
                "لا يملك المستخدم صلاحية طلب الخدمة.",
                "Caller does not hold Request Service permission.");
        }

        return null;
    }
    private static ApiResponse<T>? EnsureCanViewServiceRequests<T>(
    User currentUser)
    {
        var permissions = GetEffectivePermissions(currentUser);

        if (!permissions.Contains(ViewServiceRequestsPermission))
        {
            return ApiResponse<T>.FailureResponse(
                "لا يملك المستخدم صلاحية عرض طلبات الخدمة.",
                "Caller does not hold View Service Requests permission.");
        }

        return null;
    }
    private static HashSet<string> GetEffectivePermissions(User user)
    {
        var rolePermissions = user.Role.RolePermissions
            .Where(rp => rp.IsActive && rp.Permission.IsActive)
            .Select(rp => rp.Permission.PermissionNameEn);

        var directPermissions = user.UserPermissions
            .Where(up => up.IsActive && up.Permission.IsActive)
            .Select(up => up.Permission.PermissionNameEn);

        var groupPermissions = user.UserGroups
            .Where(ug => ug.Group.IsActive)
            .SelectMany(ug => ug.Group.GroupPermissions)
            .Where(gp => gp.IsActive && gp.Permission.IsActive)
            .Select(gp => gp.Permission.PermissionNameEn);

        return rolePermissions
            .Concat(directPermissions)
            .Concat(groupPermissions)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
    public async Task<ApiResponse<AssociationLookupDto>> ResolveAssociationByCityIdAsync(int currentUserId, int cityId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<AssociationLookupDto>(currentUserId, cancellationToken);
        if (authError is not null) { return authError; }
        var permissionError = EnsureCanRequestService<AssociationLookupDto>(currentUser!);
        if (permissionError is not null)
        {
            return permissionError;
        }
        var association = await _unitOfWork.Associations.GetByCityIdAsync(cityId, cancellationToken);
        if (association is null)
        {
            return ApiResponse<AssociationLookupDto>.FailureResponse("لم يتم العثور على جمعية مرتبطة بالمدينة المحددة.", "No association found for the specified city.");
        }
        var AssociationsInfo = new AssociationLookupDto
        {
            AssociationId = association.Id,
            EnglishName = association.EnglishName,
            ArabicName = association.ArabicName,
            CityId = association.CityId,
            Latitude = association.Latitude,
            Longitude = association.Longitude
        };
        return ApiResponse<AssociationLookupDto>.SuccessResponse(AssociationsInfo, "تم العثور على الجمعية بنجاح.", "Association found successfully.");
    }




    // Create Service Request
    public async Task<ApiResponse<ServiceRequestDto>> CreateAsync(
    int currentUserId,
    CreateServiceRequestDto dto,
    CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) =
            await GetCurrentUserAsync<ServiceRequestDto>(
                currentUserId, cancellationToken);

        if (authError is not null)
            return authError;

        var permissionError =
            EnsureCanRequestService<ServiceRequestDto>(currentUser!);

        if (permissionError is not null)
            return permissionError;

        // Validate request values
        if (dto.RequestedWorkersCount <= 0 || dto.ShiftsCount <= 0)
        {
            return ApiResponse<ServiceRequestDto>.FailureResponse(
                "عدد العمال والمدة يجب أن يكونا أكبر من صفر.",
                "Workers count and duration must be greater than zero.");
        }

        if (dto.EndDate < dto.StartDate)
        {
            return ApiResponse<ServiceRequestDto>.FailureResponse(
                "تاريخ النهاية يجب ألا يسبق تاريخ البداية.",
                "End date cannot be earlier than start date.");
        }

        // Validate project
        var project = await _unitOfWork.Projects
            .GetByIdAsync(dto.ProjectId, cancellationToken);

        if (project is null)
        {
            return ApiResponse<ServiceRequestDto>.FailureResponse(
                "لم يتم العثور على المشروع.",
                "Project was not found.");
        }

        if (currentUser!.EntityType != EntityType.ProductionCompany ||
            currentUser.EntityId != project.ProductionCompanyId)
        {
            return ApiResponse<ServiceRequestDto>.FailureResponse(
                "يجب أن ينتمي المستخدم إلى شركة الإنتاج التي تملك المشروع.",
                "Caller must belong to the production company that owns the project.");
        }

        // Find association in the requested city
        var association = await _unitOfWork.Associations
            .GetByCityIdAsync(dto.CityId, cancellationToken);

        if (association is null)
        {
            return ApiResponse<ServiceRequestDto>.FailureResponse(
                "لم يتم العثور على جمعية مرتبطة بالمدينة المحددة.",
                "No association found for the specified city.");
        }

        // Prevent duplicate requests
        //if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
        //{
        //    var existingRequest =
        //        await _unitOfWork.ServiceRequests.GetByIdempotencyKeyAsync(
        //            dto.IdempotencyKey,
        //            project.ProductionCompanyId,
        //            cancellationToken);

        //    if (existingRequest is not null)
        //    {
        //        return ApiResponse<ServiceRequestDto>.FailureResponse(
        //            "تعذرت إنشاء طلب خدمة مكرر.",
        //            "Duplicate service request detected.");
        //    }
        //}

        // Get service from database
        var service = await _unitOfWork.Services
            .GetByIdAsync(dto.ServiceId, cancellationToken);

        if (service is null)
        {
            return ApiResponse<ServiceRequestDto>.FailureResponse(
                "لم يتم العثور على الخدمة.",
                "Service was not found.");
        }

        // Validate the service's calculation type
        if (!Enum.IsDefined(typeof(CalculationType), service.CalculationType))
        {
            return ApiResponse<ServiceRequestDto>.FailureResponse(
                "نوع احتساب سعر الخدمة غير صالح.",
                "Invalid service calculation type.");
        }

        // Calculate payment using the service's actual unit price
        decimal expectedTotalAmount =
            service.Price *
            dto.ShiftsCount *
            dto.RequestedWorkersCount;

        // Create service request
        var serviceRequestInfo = new ServiceRequest
        {
            ProjectId = dto.ProjectId,
            ProductionCompanyId = project.ProductionCompanyId,
            ServiceId = service.Id,
            AssociationId = association.Id,

            StartDate = dto.StartDate,
            EndDate = dto.EndDate,

            ShiftsCount = dto.ShiftsCount,
            RequestedWorkersCount = dto.RequestedWorkersCount,
            AttendanceFrequency = dto.AttendanceFrequency,
            AdditionalRequirements = dto.AdditionalRequirements,

            //IdempotencyKey = dto.IdempotencyKey,

            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUserId,

            // Snapshot of the price at request creation time
            UnitPriceSnapshot = service.Price,

            ExpectedTotalAmount = expectedTotalAmount
        };

        await _unitOfWork.ServiceRequests.AddAsync(
            serviceRequestInfo, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send notifications
        var associationUsers = await _unitOfWork.Users.GetByEntityAsync(
            EntityType.Association,
            association.Id,
            search: null,
            cancellationToken);

        var productionCompanyUsers =
            await _unitOfWork.Users.GetByEntityAsync(
                EntityType.ProductionCompany,
                project.ProductionCompanyId,
                search: null,
                cancellationToken);

        var subject = "New service request pending worker selection";

        var body =
            $"A new service request (Id: {serviceRequestInfo.Id}) was created " +
            $"for project '{project.ProjectNameEn}' " +
            "and awaits worker selection.";

        if (!string.IsNullOrWhiteSpace(association.ContactEmail))
        {
            await _emailSender.SendAsync(
                association.ContactEmail,
                subject,
                body,
                cancellationToken);
        }

        foreach (var user in associationUsers)
        {
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                await _emailSender.SendAsync(
                    user.Email, subject, body, cancellationToken);
            }
        }

        foreach (var user in productionCompanyUsers)
        {
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                await _emailSender.SendAsync(
                    user.Email, subject, body, cancellationToken);
            }
        }

        var creationResult = new ServiceRequestDto
        {
            Id = serviceRequestInfo.Id,
            Status = serviceRequestInfo.Status
        };

        return ApiResponse<ServiceRequestDto>.SuccessResponse(
            creationResult,
            "تم إنشاء طلب الخدمة بنجاح.",
            "Service request created successfully.");
    }
    //public async Task<ApiResponse<ServiceRequestDto>> CreateAsync(int currentUserId, CreateServiceRequestDto dto, CancellationToken cancellationToken = default)
    //{
    //    var (currentUser, authError) = await GetCurrentUserAsync<ServiceRequestDto>(currentUserId, cancellationToken);
    //    if (authError is not null)
    //    {
    //        return authError;
    //    }
    //    var permissionError = EnsureCanRequestService<ServiceRequestDto>(currentUser!);
    //    if (permissionError is not null)
    //    {
    //        return permissionError;
    //    }
    //    // Validate project
    //    var Validateproject = await _unitOfWork.Projects.GetByIdAsync(dto.ProjectId, cancellationToken);
    //    if (Validateproject is null)
    //    {
    //        return ApiResponse<ServiceRequestDto>.FailureResponse("لم يتم العثور على المشروع.", "Project was not found.");
    //    }
    //    if (currentUser!.EntityType != EntityType.ProductionCompany || currentUser.EntityId != Validateproject.ProductionCompanyId)
    //    {
    //        return ApiResponse<ServiceRequestDto>.FailureResponse("يجب أن ينتمي المستخدم إلى شركة الإنتاج التي تملك المشروع.", "Caller must belong to the production company that owns the project.");
    //    } // Find association in the requested city
    //    var association = await _unitOfWork.Associations.GetByCityIdAsync(dto.CityId, cancellationToken);
    //    if (association is null)
    //    {
    //        return  ApiResponse<ServiceRequestDto>.FailureResponse("لم يتم العثور على جمعية مرتبطة بالمدينة المحددة.", "No association found for the specified city.");
    //    }
    //    // Idempotency / duplicate prevention
    //    if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
    //    {
    //        var existingRequest = await _unitOfWork.ServiceRequests.GetByIdempotencyKeyAsync(dto.IdempotencyKey, Validateproject.ProductionCompanyId, cancellationToken);
    //        if (existingRequest is not null)
    //        {
    //            return ApiResponse<ServiceRequestDto>.FailureResponse("تعذرت إنشاء طلب خدمة مكرر.", "Duplicate service request detected.");
    //        }
    //    }
    //    // Get service
    //    var service = await _unitOfWork.Services.GetByIdAsync(dto.ServiceId, cancellationToken); if (service is null)
    //    {
    //        return ApiResponse<ServiceRequestDto>.FailureResponse("لم يتم العثور على الخدمة.", "Service was not found.");
    //    }
    //    var ServiceRequestInfo = new ServiceRequest
    //    {
    //        ProjectId = dto.ProjectId,
    //        ProductionCompanyId = Validateproject.ProductionCompanyId,
    //        ServiceId = dto.ServiceId,
    //        AssociationId = association.Id,
    //        StartDate = dto.StartDate,
    //        EndDate = dto.EndDate,
    //        TimeUnit = dto.TimeUnit,
    //        ShiftsCount = dto.ShiftsCount,
    //        RequestedWorkersCount = dto.RequestedWorkersCount,
    //        AttendanceFrequency = dto.AttendanceFrequency,
    //        AdditionalRequirements = dto.AdditionalRequirements,
    //        IdempotencyKey = dto.IdempotencyKey,
    //        CreatedAt = DateTime.UtcNow,
    //        CreatedBy = currentUserId,
    //        //this make sure that the price is selected from the database.86
    //        UnitPriceSnapshot = service.Price

    //    };
    //    //resultOfServiceRequestDto.UnitPriceSnapshot = service.Price;
    //    // Calculate expected total amount
    //    switch (dto.TimeUnit)
    //    {
    //        case ServiceTimeUnit.Shift:
    //            // Shift = 12 hours
    //            ServiceRequestInfo.ExpectedTotalAmount = (12 * service.Price) * dto.RequestedWorkersCount * dto.ShiftsCount;
    //            break;
    //        case ServiceTimeUnit.Day:
    //            // Day = 9 hours
    //            ServiceRequestInfo.ExpectedTotalAmount = (9 * service.Price) * dto.RequestedWorkersCount * dto.ShiftsCount;
    //            break;
    //        case ServiceTimeUnit.Hour:
    //            // Hour = direct calculation
    //            ServiceRequestInfo.ExpectedTotalAmount = (service.Price * dto.ShiftsCount) * dto.RequestedWorkersCount;
    //            break;
    //        default: return ApiResponse<ServiceRequestDto>.FailureResponse("وحدة الوقت غير صالحة.", "Invalid time unit.");
    //    }
    //    await _unitOfWork.ServiceRequests.AddAsync(ServiceRequestInfo, cancellationToken); await _unitOfWork.SaveChangesAsync(cancellationToken);
    //    // ======================================================== // Notifications // ========================================================

    //    var assocUsers = await _unitOfWork.Users.GetByEntityAsync(EntityType.Association, association.Id, search: null, cancellationToken);
    //    var prodUsers = await _unitOfWork.Users.GetByEntityAsync(EntityType.ProductionCompany, Validateproject.ProductionCompanyId, search: null, cancellationToken);
    //    var subject = "New service request pending worker selection";
    //    var body = $"A new service request (Id: {ServiceRequestInfo.Id}) was created " + $"for project '{Validateproject.ProjectNameEn}' " + $"and awaits worker selection.";
    //    if (!string.IsNullOrWhiteSpace(association.ContactEmail))
    //    {
    //        await _emailSender.SendAsync(association.ContactEmail, subject, body, cancellationToken);
    //    }
    //    foreach (var user in assocUsers)
    //    {
    //        if (!string.IsNullOrWhiteSpace(user.Email))
    //        {
    //            await _emailSender.SendAsync(user.Email, subject, body, cancellationToken);
    //        }
    //    }
    //    foreach (var user in prodUsers)
    //    {
    //        if (!string.IsNullOrWhiteSpace(user.Email))
    //        {
    //            await _emailSender.SendAsync(user.Email, subject, body, cancellationToken);
    //        }
    //    }
    //    var CreationResult = new ServiceRequestDto { Id = ServiceRequestInfo.Id, Status = ServiceRequestInfo.Status };
    //    return ApiResponse<ServiceRequestDto>.SuccessResponse(CreationResult, "تم إنشاء طلب الخدمة بنجاح.", "Service request created successfully.");
    //}
    public async Task<ApiResponse<List<ServiceRequestDto>>> GetAllAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<List<ServiceRequestDto>>(currentUserId, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }
        var permissionError = EnsureCanViewServiceRequests<List<ServiceRequestDto>>(currentUser!);
        if (permissionError is not null) { 
            return permissionError;
        }
        List<ServiceRequest> AllServiceRequests;
        if (currentUser!.EntityType == EntityType.ProductionCompany)
        {
            AllServiceRequests = await _unitOfWork.ServiceRequests.GetByProductionCompanyIdAsync(currentUser.EntityId, cancellationToken);
        }
        else if (currentUser.EntityType == EntityType.Association)
        {
            AllServiceRequests = await _unitOfWork.ServiceRequests.GetByAssociationIdAsync(currentUser.EntityId, cancellationToken);
        }
        else
        {
            AllServiceRequests = new List<ServiceRequest>();
        }
        var response = AllServiceRequests.Select(r =>
            new ServiceRequestDto { Id = r.Id, Status = r.Status }).ToList();
        return ApiResponse<List<ServiceRequestDto>>.SuccessResponse(response, "تم جلب طلبات الخدمة بنجاح.", "Service requests retrieved successfully.");
    }
    public async Task<ApiResponse<ServiceRequestDetailsDto>> GetByIdAsync(int currentUserId, int id, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<ServiceRequestDetailsDto>(currentUserId, cancellationToken); if (authError is not null)
        {
            return authError;
        }
        var permissionError = EnsureCanViewServiceRequests<ServiceRequestDetailsDto>(currentUser!);
        if (permissionError is not null)
        {
            return permissionError;
        }
        var CurrentRequestId = await _unitOfWork.ServiceRequests.GetByIdAsync(id, cancellationToken); if (CurrentRequestId is null)
        {
            return ApiResponse<ServiceRequestDetailsDto>.FailureResponse("لم يتم العثور على طلب الخدمة.", "Service request was not found.");
        } // Ensure caller is allowed to view this request
        if (currentUser!.EntityType == EntityType.ProductionCompany && currentUser.EntityId != CurrentRequestId.ProductionCompanyId)
        {
            return ApiResponse<ServiceRequestDetailsDto>.FailureResponse("ليس لديك الصلاحية لعرض طلب الخدمة هذا.", "Caller is not authorized to view this service request.");
        }
        if (currentUser.EntityType == EntityType.Association && currentUser.EntityId != CurrentRequestId.AssociationId)
        {
            return ApiResponse<ServiceRequestDetailsDto>.FailureResponse("ليس لديك الصلاحية لعرض طلب الخدمة هذا.", "Caller is not authorized to view this service request.");
        }
        var ServiceRequestDetails = new ServiceRequestDetailsDto
        {
            Id = CurrentRequestId.Id,
            ServiceId = CurrentRequestId.ServiceId,
            ServiceNameEn = CurrentRequestId.Service?.NameEn ?? string.Empty,
            ServiceNameAr = CurrentRequestId.Service?.NameAr ?? string.Empty,
            ProjectId = CurrentRequestId.ProjectId,
            ProjectNameEn = CurrentRequestId.Project?.ProjectNameEn ?? string.Empty,
            ProjectNameAr = CurrentRequestId.Project?.ProjectNameAr ?? string.Empty,
            AssociationId = CurrentRequestId.AssociationId,
            AssociationNameEn = CurrentRequestId.Association?.EnglishName ?? string.Empty,
            AssociationNameAr = CurrentRequestId.Association?.ArabicName ?? string.Empty,
            CityId = CurrentRequestId.Association?.CityId ?? 0,
            StartDate = CurrentRequestId.StartDate,
            EndDate = CurrentRequestId.EndDate,
            ShiftsCount = CurrentRequestId.ShiftsCount,
            RequestedWorkersCount = CurrentRequestId.RequestedWorkersCount,
            SelectedWorkersCount = CurrentRequestId.SelectedWorkersCount,
            AttendanceFrequency = CurrentRequestId.AttendanceFrequency,
            UnitPriceSnapshot = CurrentRequestId.UnitPriceSnapshot,
            ExpectedTotalAmount = CurrentRequestId.ExpectedTotalAmount,
// get assigned workers with their documents
            AssignedWorkers = CurrentRequestId.ServiceRequestWorkers
            .Select(srw => new AssignedWorkerDto(
            srw.Worker.Id,
            $"{srw.Worker.FirstName} {srw.Worker.LastName}".Trim(),
            srw.Worker.PhoneNumber,
            srw.Worker.Nationality,
            srw.Worker.YearsOfExperience,
            srw.Worker.Documents.Select(d => d.FilePath).ToList())).ToList(),

            Status = CurrentRequestId.Status,
            CreatedAt = CurrentRequestId.CreatedAt,
            CancelledAt = CurrentRequestId.CancelledAt,
            AdditionalRequirements = CurrentRequestId.AdditionalRequirements
        };
        return ApiResponse<ServiceRequestDetailsDto>.SuccessResponse(ServiceRequestDetails, "تم جلب طلب الخدمة بنجاح.", "Service request retrieved successfully.");
    }

    public async Task<ApiResponse<object?>> CancelAsync(int currentUserId, int id, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<object?>(currentUserId, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }
        var permissionError = EnsureCanRequestService<object?>(currentUser!);
        if (permissionError is not null)
        {
            return permissionError;
        }
        var CanceledRequestId = await _unitOfWork.ServiceRequests.GetByIdAsync(id, cancellationToken);
        if (CanceledRequestId is null)
        {
            return ApiResponse<object?>.FailureResponse("لم يتم العثور على طلب الخدمة.", "Service request was not found.");
        }
        // Admin can cancel any request. 
        // Otherwise, caller must be the owning production company.
        var effective = GetEffectivePermissions(currentUser!);
        bool isAdmin = effective.Contains(ManageServiceRequestsPermission);
        if (!isAdmin)
        {
            if (currentUser!.EntityType != EntityType.ProductionCompany || currentUser.EntityId != CanceledRequestId.ProductionCompanyId)
            {
                return ApiResponse<object?>.FailureResponse("ليس لديك الصلاحية لإلغاء طلب الخدمة هذا.", "Only the owning production company may cancel this request.");
            }
        }
        if (CanceledRequestId.Status != ServiceRequestStatus.PendingWorkerSelection)
        {
            return ApiResponse<object?>.FailureResponse("لا تستطيع الغاء طلبك ", "Cannot cancel the request after worker assignment has started.");
        }
        CanceledRequestId.Status = ServiceRequestStatus.Cancelled; CanceledRequestId.CancelledAt = DateTime.UtcNow; await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ApiResponse<object?>.SuccessResponse(null, "تم إلغاء طلب الخدمة بنجاح.", "Service request cancelled successfully.");
    }



    //public async Task<ApiResponse<ExpectedPaymentCalculationDto>> CalculateExpectedPaymentAsync(int currentUserId, int serviceId, int requestedWorkers, int durationCount, ServiceTimeUnit timeUnit, CancellationToken cancellationToken = default)
    //{
    //    var (currentUser, authError) = await GetCurrentUserAsync<ExpectedPaymentCalculationDto>(currentUserId, cancellationToken);
    //    if (authError is not null)
    //    {
    //        return authError;
    //    }
    //    var permissionError = EnsureCanRequestService<ExpectedPaymentCalculationDto>(currentUser!);
    //    if (permissionError is not null) { return permissionError; }
    //    var service = await _unitOfWork.Services.GetByIdAsync(serviceId, cancellationToken);
    //    if (service is null)
    //    {
    //        return ApiResponse<ExpectedPaymentCalculationDto>.FailureResponse("لم يتم العثور على الخدمة.", "Service was not found.");
    //    }
    //    decimal totalExpected; switch (timeUnit)
    //    {
    //        case ServiceTimeUnit.Shift:
    //            // Shift = 12 hours
    //            totalExpected = (12 * service.Price) * requestedWorkers * durationCount;
    //            break;
    //        case ServiceTimeUnit.Day:
    //            // Day = 9 hours
    //            totalExpected = (9 * service.Price) * requestedWorkers * durationCount;
    //            break;
    //        case ServiceTimeUnit.Hour:
    //            // Hour = direct calculation
    //            totalExpected = (service.Price * durationCount) * requestedWorkers;
    //            break;
    //        default: return ApiResponse<ExpectedPaymentCalculationDto>.FailureResponse("وحدة الوقت غير صالحة.", "Invalid time unit.");
    //    }
    //    string timeUnitLabel = timeUnit switch
    //    {
    //        ServiceTimeUnit.Shift => "shifts (12 hours each)",
    //        ServiceTimeUnit.Day => "days (9 hours each)",
    //        ServiceTimeUnit.Hour => "hours",
    //        _ => "unknown"
    //    }; var CalculationInfo = new ExpectedPaymentCalculationDto
    //    {
    //        UnitPrice = service.Price,
    //        RequestedWorkers = requestedWorkers,
    //        DurationUnits = durationCount,
    //        TotalExpectedPayment = totalExpected,
    //        FormattedMessage = $"Expected payment for {requestedWorkers} workers " + $"for {durationCount} {timeUnitLabel}: " + $"{totalExpected:N2} JOD."
    //    };
    //    return ApiResponse<ExpectedPaymentCalculationDto>.SuccessResponse(CalculationInfo, "تم حساب المبلغ المتوقع بنجاح.", "Expected payment calculated successfully.");
    //}

    public async Task<ApiResponse<ExpectedPaymentCalculationDto>>CalculateExpectedPaymentAsync(
        int currentUserId,
        int serviceId,
        int requestedWorkers,
        int durationCount,
        CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) =
            await GetCurrentUserAsync<ExpectedPaymentCalculationDto>(
                currentUserId, cancellationToken);

        if (authError is not null)
            return authError;

        var permissionError =
            EnsureCanRequestService<ExpectedPaymentCalculationDto>(currentUser!);

        if (permissionError is not null)
            return permissionError;

        var service = await _unitOfWork.Services
            .GetByIdAsync(serviceId, cancellationToken);

        if (service is null)
        {
            return ApiResponse<ExpectedPaymentCalculationDto>
                .FailureResponse(
                    "لم يتم العثور على الخدمة.",
                    "Service was not found.");
        }

        if (requestedWorkers <= 0 || durationCount <= 0)
        {
            return ApiResponse<ExpectedPaymentCalculationDto>
                .FailureResponse(
                    "عدد العمال والمدة يجب أن يكونا أكبر من صفر.",
                    "Workers count and duration must be greater than zero.");
        }

        if (!Enum.IsDefined(typeof(CalculationType), service.CalculationType))
        {
            return ApiResponse<ExpectedPaymentCalculationDto>
                .FailureResponse(
                    "نوع احتساب سعر الخدمة غير صالح.",
                    "Invalid service calculation type.");
        }

        // Price is already the price per hour, shift, or day.
        decimal totalExpected =
            service.Price * requestedWorkers * durationCount;

        string timeUnitLabel = service.CalculationType switch
        {
            CalculationType.PerHour => "hours",
            CalculationType.PerShift => "shifts",
            CalculationType.PerDay => "days",
            _ => "units"
        };

        var calculationInfo = new ExpectedPaymentCalculationDto
        {
            UnitPrice = service.Price,
            RequestedWorkers = requestedWorkers,
            DurationUnits = durationCount,
            TotalExpectedPayment = totalExpected,

            FormattedMessage =
                $"Expected payment for {requestedWorkers} workers " +
                $"for {durationCount} {timeUnitLabel}: " +
                $"{totalExpected:N2} JOD."
        };

        return ApiResponse<ExpectedPaymentCalculationDto>
            .SuccessResponse(
                calculationInfo,
                "تم حساب المبلغ المتوقع بنجاح.",
                "Expected payment calculated successfully.");
    }
    public async Task<ApiResponse<object?>> RejectAsync(int currentUserId, int id, string? reason, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<object?>(currentUserId, cancellationToken);
        if (authError is not null)
            return authError;
        var RejectedRequestId = await _unitOfWork.ServiceRequests.GetByIdAsync(id, cancellationToken);
        if (RejectedRequestId is null)
            return ApiResponse<object?>.FailureResponse("لم يتم العثور على طلب الخدمة.", "Service request was not found.");

        // Validate caller is the Association assigned to this request
        if (currentUser!.EntityType != EntityType.Association || currentUser.EntityId != RejectedRequestId.AssociationId)
        {
            return ApiResponse<object?>.FailureResponse("ليس لديك الصلاحية لرفض طلب الخدمة هذا.", "Only the assigned association may reject this request.");
        }
        if (RejectedRequestId.Status != ServiceRequestStatus.PendingWorkerSelection)
        {
            return ApiResponse<object?>.FailureResponse("لا يمكن رفض الطلب في حالته الحالية.", "Cannot reject request in its current status.");
        }
        RejectedRequestId.Status = ServiceRequestStatus.Rejected;
        // If you add a RejectionReason property to ServiceRequest entity, you can set it here:
        //sr.RejectionReason = reason;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        // Optional: Send Email to Production Company here that their request was rejected.
        return ApiResponse<object?>.SuccessResponse(null, "تم رفض طلب الخدمة بنجاح.", "Service request rejected successfully.");
    }
    //public async Task<ApiResponse<object?>> ApproveAsync(int currentUserId, int id, CancellationToken cancellationToken = default)
    //{
    //    var (currentUser, authError) = await GetCurrentUserAsync<object?>(currentUserId, cancellationToken);
    //    if (authError is not null)
    //        return authError;
    //    var sr = await _unitOfWork.ServiceRequests.GetByIdAsync(id, cancellationToken);
    //    if (sr is null)
    //        return ApiResponse<object?>.FailureResponse("لم يتم العثور على طلب الخدمة.", "Service request was not found.");
    //    // Validate caller is the Association
    //    if (currentUser!.EntityType != EntityType.Association || currentUser.EntityId != sr.AssociationId)
    //    {
    //        return ApiResponse<object?>.FailureResponse("ليس لديك الصلاحية للموافقة على طلب الخدمة هذا.", "Only the assigned association may approve this request.");
    //    }
    //    if (sr.Status != ServiceRequestStatus.PendingWorkerSelection)
    //    {
    //        return ApiResponse<object?>.FailureResponse("لا يمكن الموافقة على الطلب في حالته الحالية.", "Cannot approve request in its current status.");
    //    }
    //    sr.Status = ServiceRequestStatus.InProgress;
    //    await _unitOfWork.SaveChangesAsync(cancellationToken);
    //    // Optional: Send Email to Production Company here that their request was approved.
    //    return ApiResponse<object?>.SuccessResponse(null, "تمت الموافقة على طلب الخدمة بنجاح.", "Service request approved successfully.");
    //}


    public async Task<ApiResponse<object?>> ApproveAndAssignAsync(int currentUserId, int id, List<int> workerIds, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<object?>(currentUserId, cancellationToken);
        if (authError is not null) return authError;

        var CurrentRequestToApprove = await _unitOfWork.ServiceRequests.GetByIdAsync(id, cancellationToken);
        

        if (CurrentRequestToApprove is null)
        {
            return ApiResponse<object?>.FailureResponse(
                "Service request was not found.",
                "طلب الخدمة غير موجود.");
        }
        if (currentUser!.EntityType != EntityType.Association || currentUser.EntityId != CurrentRequestToApprove.AssociationId)
                {
                    return ApiResponse<object?>.FailureResponse("ليس لديك الصلاحية للموافقة على طلب الخدمة هذا.", "Only the assigned association may approve this request.");
               }
       
        // Make sure the request is in the correct status
        if (CurrentRequestToApprove.Status != ServiceRequestStatus.PendingWorkerSelection)
        {
            return ApiResponse<object?>.FailureResponse(
                "Cannot approve and assign workers for the request in its current status.",
                "لا يمكن الموافقة وتعيين العمال في حالة الطلب الحالية.");
        }
        if (workerIds is null || workerIds.Count != CurrentRequestToApprove.RequestedWorkersCount)
        {
            return ApiResponse<object?>.FailureResponse("عدد العمال المحدد لا يتطابق مع العدد المطلوب.", "Worker count mismatch.");
        }

        // Reject duplicate worker IDs
        if (workerIds is null ||
            workerIds.Count != workerIds.Distinct().Count())
        {
            return ApiResponse<object?>.FailureResponse(
                "Worker IDs must be unique.",
                "يجب عدم تكرار العمال.");
        }

        foreach (var workerId in workerIds)
        {
            var worker = await _unitOfWork.Workers
                .GetByIdAsync(workerId, cancellationToken);

            if (worker is null)
            {
                return ApiResponse<object?>.FailureResponse(
                    $"Worker with ID {workerId} was not found.",
                    $"العامل ذو الرقم {workerId} غير موجود.");
            }

            if (worker.AssociationId != CurrentRequestToApprove.AssociationId)
            {
                return ApiResponse<object?>.FailureResponse(
                    $"Worker {workerId} does not belong to the request association.",
                    $"العامل ذو الرقم {workerId} لا يتبع الجمعية المرتبطة بالطلب.");
            }

            await _unitOfWork.ServiceRequests
            .AssignToServiceRequestAsync(id,workerId,cancellationToken);
        }


        CurrentRequestToApprove.SelectedWorkersCount = workerIds.Count;
        CurrentRequestToApprove.Status = ServiceRequestStatus.InProgress;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object?>.SuccessResponse(null, "تمت الموافقة وتعيين العمال بنجاح.", "Workers assigned successfully.");
    }

}
