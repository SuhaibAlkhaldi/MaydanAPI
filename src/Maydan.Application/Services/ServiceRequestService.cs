using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.ServiceRequests;
using Maydan.Application.Exceptions;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Services;

public class ServiceRequestService : IServiceRequestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;

    private const int RequestServicePermissionId = 15;
    private const int ViewServiceRequestsPermissionId = 16;
    private const int ManageServiceRequestsPermissionId = 17;

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
        var permissions = GetEffectivePermissionIds(currentUser);
        if (!permissions.Contains(RequestServicePermissionId) && !permissions.Contains(ManageServiceRequestsPermissionId))
        {
            return ApiResponse<T>.FailureResponse("لا يملك المستخدم صلاحية طلب الخدمة.", "Caller does not hold Request Service permission.");
        }
        return null;
    }
    private static ApiResponse<T>? EnsureCanViewServiceRequests<T>(User currentUser)
    {
        var permissions = GetEffectivePermissionIds(currentUser);
        if (!permissions.Contains(ViewServiceRequestsPermissionId) && !permissions.Contains(ManageServiceRequestsPermissionId))
        {
            return
                ApiResponse<T>.FailureResponse("لا يملك المستخدم صلاحية عرض طلبات الخدمة.", "Caller does not hold View Service Requests permission.");
        }
        return null;
    }
    private static HashSet<int> GetEffectivePermissionIds(User user)
    {
        var rolePermissionIds = user.Role.RolePermissions.Where(rp => rp.IsActive && rp.Permission.IsActive).Select(rp => rp.PermissionId); var directPermissionIds = user.UserPermissions.Where(up => up.IsActive && up.Permission.IsActive).Select(up => up.PermissionId); var groupPermissionIds = user.UserGroups.Where(ug => ug.Group.IsActive).SelectMany(ug => ug.Group.GroupPermissions).Where(gp => gp.IsActive && gp.Permission.IsActive).Select(gp => gp.PermissionId);
        return rolePermissionIds.Concat(directPermissionIds).Concat(groupPermissionIds).ToHashSet();
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
        var resultOfAssociationLookupDto = new AssociationLookupDto
        {
            AssociationId = association.Id,
            EnglishName = association.EnglishName,
            ArabicName = association.ArabicName,
            CityId = association.CityId,
            Latitude = association.Latitude,
            Longitude = association.Longitude
        };
        return ApiResponse<AssociationLookupDto>.SuccessResponse(resultOfAssociationLookupDto, "تم العثور على الجمعية بنجاح.", "Association found successfully.");
    }




    // Create Service Request
    public async Task<ApiResponse<ServiceRequestDto>> CreateAsync(int currentUserId, CreateServiceRequestDto dto, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<ServiceRequestDto>(currentUserId, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }
        var permissionError = EnsureCanRequestService<ServiceRequestDto>(currentUser!);
        if (permissionError is not null)
        {
            return permissionError;
        }
        // Validate project
        var Validateproject = await _unitOfWork.Projects.GetByIdAsync(dto.ProjectId, cancellationToken);
        if (Validateproject is null)
        {
            return ApiResponse<ServiceRequestDto>.FailureResponse("لم يتم العثور على المشروع.", "Project was not found.");
        }
        if (currentUser!.EntityType != EntityType.ProductionCompany || currentUser.EntityId != Validateproject.ProductionCompanyId)
        {
            return ApiResponse<ServiceRequestDto>.FailureResponse("يجب أن ينتمي المستخدم إلى شركة الإنتاج التي تملك المشروع.", "Caller must belong to the production company that owns the project.");
        } // Find association in the requested city
        var association = await _unitOfWork.Associations.GetByCityIdAsync(dto.CityId, cancellationToken);
        if (association is null)
        {
            return  ApiResponse<ServiceRequestDto>.FailureResponse("لم يتم العثور على جمعية مرتبطة بالمدينة المحددة.", "No association found for the specified city.");
        }
        // Idempotency / duplicate prevention
        if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
        {
            var existingRequest = await _unitOfWork.ServiceRequests.GetByIdempotencyKeyAsync(dto.IdempotencyKey, Validateproject.ProductionCompanyId, cancellationToken);
            if (existingRequest is not null)
            {
                return ApiResponse<ServiceRequestDto>.FailureResponse("تعذرت إنشاء طلب خدمة مكرر.", "Duplicate service request detected.");
            }
        }
        // Get service
        var service = await _unitOfWork.Services.GetByIdAsync(dto.ServiceId, cancellationToken); if (service is null)
        {
            return ApiResponse<ServiceRequestDto>.FailureResponse("لم يتم العثور على الخدمة.", "Service was not found.");
        }
        var resultOfServiceRequestDto = new ServiceRequest
        {
            ProjectId = dto.ProjectId,
            ProductionCompanyId = Validateproject.ProductionCompanyId,
            ServiceId = dto.ServiceId,
            AssociationId = association.Id,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            TimeUnit = dto.TimeUnit,
            ShiftsCount = dto.ShiftsCount,
            RequestedWorkersCount = dto.RequestedWorkersCount,
            AttendanceFrequency = dto.AttendanceFrequency,
            AdditionalRequirements = dto.AdditionalRequirements,
            IdempotencyKey = dto.IdempotencyKey,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUserId,
            //this make sure that the price is selected from the database.86
            UnitPriceSnapshot = service.Price

        };
        //resultOfServiceRequestDto.UnitPriceSnapshot = service.Price;
        // Calculate expected total amount
        switch (dto.TimeUnit)
        {
            case ServiceTimeUnit.Shift:
                // Shift = 12 hours
                resultOfServiceRequestDto.ExpectedTotalAmount = (12 * service.Price) * dto.RequestedWorkersCount * dto.ShiftsCount;
                break;
            case ServiceTimeUnit.Day:
                // Day = 9 hours
                resultOfServiceRequestDto.ExpectedTotalAmount = (9 * service.Price) * dto.RequestedWorkersCount * dto.ShiftsCount;
                break;
            case ServiceTimeUnit.Hour:
                // Hour = direct calculation
                resultOfServiceRequestDto.ExpectedTotalAmount = (service.Price * dto.ShiftsCount) * dto.RequestedWorkersCount;
                break;
            default: return ApiResponse<ServiceRequestDto>.FailureResponse("وحدة الوقت غير صالحة.", "Invalid time unit.");
        }
        await _unitOfWork.ServiceRequests.AddAsync(resultOfServiceRequestDto, cancellationToken); await _unitOfWork.SaveChangesAsync(cancellationToken);
        // ======================================================== // Notifications // ========================================================

        var assocUsers = await _unitOfWork.Users.GetByEntityAsync(EntityType.Association, association.Id, search: null, cancellationToken);
        var prodUsers = await _unitOfWork.Users.GetByEntityAsync(EntityType.ProductionCompany, Validateproject.ProductionCompanyId, search: null, cancellationToken);
        var subject = "New service request pending worker selection";
        var body = $"A new service request (Id: {resultOfServiceRequestDto.Id}) was created " + $"for project '{Validateproject.ProjectNameEn}' " + $"and awaits worker selection.";
        if (!string.IsNullOrWhiteSpace(association.ContactEmail))
        {
            await _emailSender.SendAsync(association.ContactEmail, subject, body, cancellationToken);
        }
        foreach (var user in assocUsers)
        {
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                await _emailSender.SendAsync(user.Email, subject, body, cancellationToken);
            }
        }
        foreach (var user in prodUsers)
        {
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                await _emailSender.SendAsync(user.Email, subject, body, cancellationToken);
            }
        }
        var result = new ServiceRequestDto { Id = resultOfServiceRequestDto.Id, Status = resultOfServiceRequestDto.Status };
        return ApiResponse<ServiceRequestDto>.SuccessResponse(result, "تم إنشاء طلب الخدمة بنجاح.", "Service request created successfully.");
    }
    public async Task<ApiResponse<List<ServiceRequestDto>>> GetAllAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<List<ServiceRequestDto>>(currentUserId, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }
        var permissionError = EnsureCanViewServiceRequests<List<ServiceRequestDto>>(currentUser!);
        if (permissionError is not null) { return permissionError; }
        List<ServiceRequest> results;
        if (currentUser!.EntityType == EntityType.ProductionCompany)
        {
            results = await _unitOfWork.ServiceRequests.GetByProductionCompanyIdAsync(currentUser.EntityId, cancellationToken);
        }
        else if (currentUser.EntityType == EntityType.Association)
        {
            results = await _unitOfWork.ServiceRequests.GetByAssociationIdAsync(currentUser.EntityId, cancellationToken);
        }
        else
        {
            results = new List<ServiceRequest>();
        }
        var response = results.Select(r =>
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
        var VarOfCurrentRequestId = await _unitOfWork.ServiceRequests.GetByIdAsync(id, cancellationToken); if (VarOfCurrentRequestId is null)
        {
            return ApiResponse<ServiceRequestDetailsDto>.FailureResponse("لم يتم العثور على طلب الخدمة.", "Service request was not found.");
        } // Ensure caller is allowed to view this request
        if (currentUser!.EntityType == EntityType.ProductionCompany && currentUser.EntityId != VarOfCurrentRequestId.ProductionCompanyId)
        {
            return ApiResponse<ServiceRequestDetailsDto>.FailureResponse("ليس لديك الصلاحية لعرض طلب الخدمة هذا.", "Caller is not authorized to view this service request.");
        }
        if (currentUser.EntityType == EntityType.Association && currentUser.EntityId != VarOfCurrentRequestId.AssociationId)
        {
            return ApiResponse<ServiceRequestDetailsDto>.FailureResponse("ليس لديك الصلاحية لعرض طلب الخدمة هذا.", "Caller is not authorized to view this service request.");
        }
        var result = new ServiceRequestDetailsDto
        {
            Id = VarOfCurrentRequestId.Id,
            ServiceId = VarOfCurrentRequestId.ServiceId,
            ServiceNameEn = VarOfCurrentRequestId.Service?.NameEn ?? string.Empty,
            ServiceNameAr = VarOfCurrentRequestId.Service?.NameAr ?? string.Empty,
            ProjectId = VarOfCurrentRequestId.ProjectId,
            ProjectNameEn = VarOfCurrentRequestId.Project?.ProjectNameEn ?? string.Empty,
            ProjectNameAr = VarOfCurrentRequestId.Project?.ProjectNameAr ?? string.Empty,
            AssociationId = VarOfCurrentRequestId.AssociationId,
            AssociationNameEn = VarOfCurrentRequestId.Association?.EnglishName ?? string.Empty,
            AssociationNameAr = VarOfCurrentRequestId.Association?.ArabicName ?? string.Empty,
            CityId = VarOfCurrentRequestId.Association?.CityId ?? 0,
            StartDate = VarOfCurrentRequestId.StartDate,
            EndDate = VarOfCurrentRequestId.EndDate,
            ShiftsCount = VarOfCurrentRequestId.ShiftsCount,
            RequestedWorkersCount = VarOfCurrentRequestId.RequestedWorkersCount,
            SelectedWorkersCount = VarOfCurrentRequestId.SelectedWorkersCount,
            AttendanceFrequency = VarOfCurrentRequestId.AttendanceFrequency,
            UnitPriceSnapshot = VarOfCurrentRequestId.UnitPriceSnapshot,
            ExpectedTotalAmount = VarOfCurrentRequestId.ExpectedTotalAmount,
            Status = VarOfCurrentRequestId.Status,
            CreatedAt = VarOfCurrentRequestId.CreatedAt,
            CancelledAt = VarOfCurrentRequestId.CancelledAt,
            AdditionalRequirements = VarOfCurrentRequestId.AdditionalRequirements
        };
        return ApiResponse<ServiceRequestDetailsDto>.SuccessResponse(result, "تم جلب طلب الخدمة بنجاح.", "Service request retrieved successfully.");
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
        var VarOfCanceledRequestId = await _unitOfWork.ServiceRequests.GetByIdAsync(id, cancellationToken);
        if (VarOfCanceledRequestId is null)
        {
            return ApiResponse<object?>.FailureResponse("لم يتم العثور على طلب الخدمة.", "Service request was not found.");
        }
        // Admin can cancel any request. 
        // Otherwise, caller must be the owning production company.
        var effective = GetEffectivePermissionIds(currentUser!);
        bool isAdmin = effective.Contains(ManageServiceRequestsPermissionId);
        if (!isAdmin)
        {
            if (currentUser!.EntityType != EntityType.ProductionCompany || currentUser.EntityId != VarOfCanceledRequestId.ProductionCompanyId)
            {
                return ApiResponse<object?>.FailureResponse("ليس لديك الصلاحية لإلغاء طلب الخدمة هذا.", "Only the owning production company may cancel this request.");
            }
        }
        if (VarOfCanceledRequestId.Status != ServiceRequestStatus.PendingWorkerSelection)
        {
            return ApiResponse<object?>.FailureResponse("لا تستطيع الغاء طلبك ", "Cannot cancel the request after worker assignment has started.");
        }
        VarOfCanceledRequestId.Status = ServiceRequestStatus.Cancelled; VarOfCanceledRequestId.CancelledAt = DateTime.UtcNow; await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ApiResponse<object?>.SuccessResponse(null, "تم إلغاء طلب الخدمة بنجاح.", "Service request cancelled successfully.");
    }

   

    public async Task<ApiResponse<ExpectedPaymentCalculationDto>> CalculateExpectedPaymentAsync(int currentUserId, int serviceId, int requestedWorkers, int durationCount, ServiceTimeUnit timeUnit, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<ExpectedPaymentCalculationDto>(currentUserId, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }
        var permissionError = EnsureCanRequestService<ExpectedPaymentCalculationDto>(currentUser!);
        if (permissionError is not null) { return permissionError; }
        var service = await _unitOfWork.Services.GetByIdAsync(serviceId, cancellationToken);
        if (service is null)
        {
            return ApiResponse<ExpectedPaymentCalculationDto>.FailureResponse("لم يتم العثور على الخدمة.", "Service was not found.");
        }
        decimal totalExpected; switch (timeUnit)
        {
            case ServiceTimeUnit.Shift:
                // Shift = 12 hours
                totalExpected = (12 * service.Price) * requestedWorkers * durationCount;
                break;
            case ServiceTimeUnit.Day:
                // Day = 9 hours
                totalExpected = (9 * service.Price) * requestedWorkers * durationCount;
                break;
            case ServiceTimeUnit.Hour:
                // Hour = direct calculation
                totalExpected = (service.Price * durationCount) * requestedWorkers;
                break;
            default: return ApiResponse<ExpectedPaymentCalculationDto>.FailureResponse("وحدة الوقت غير صالحة.", "Invalid time unit.");
        }
        string timeUnitLabel = timeUnit switch
        {
            ServiceTimeUnit.Shift => "shifts (12 hours each)",
            ServiceTimeUnit.Day => "days (9 hours each)",
            ServiceTimeUnit.Hour => "hours",
            _ => "unknown"
        }; var result = new ExpectedPaymentCalculationDto
        {
            UnitPrice = service.Price,
            RequestedWorkers = requestedWorkers,
            DurationUnits = durationCount,
            TotalExpectedPayment = totalExpected,
            FormattedMessage = $"Expected payment for {requestedWorkers} workers " + $"for {durationCount} {timeUnitLabel}: " + $"{totalExpected:N2} JOD."
        };
        return ApiResponse<ExpectedPaymentCalculationDto>.SuccessResponse(result, "تم حساب المبلغ المتوقع بنجاح.", "Expected payment calculated successfully.");
    }

    public async Task<ApiResponse<object?>> RejectAsync(int currentUserId, int id, string? reason, CancellationToken cancellationToken = default)
    {
        var (currentUser, authError) = await GetCurrentUserAsync<object?>(currentUserId, cancellationToken);
        if (authError is not null)
            return authError;
        var VarForRejectedRequestId = await _unitOfWork.ServiceRequests.GetByIdAsync(id, cancellationToken);
        if (VarForRejectedRequestId is null)
            return ApiResponse<object?>.FailureResponse("لم يتم العثور على طلب الخدمة.", "Service request was not found.");

        // Validate caller is the Association assigned to this request
        if (currentUser!.EntityType != EntityType.Association || currentUser.EntityId != VarForRejectedRequestId.AssociationId)
        {
            return ApiResponse<object?>.FailureResponse("ليس لديك الصلاحية لرفض طلب الخدمة هذا.", "Only the assigned association may reject this request.");
        }
        if (VarForRejectedRequestId.Status != ServiceRequestStatus.PendingWorkerSelection)
        {
            return ApiResponse<object?>.FailureResponse("لا يمكن رفض الطلب في حالته الحالية.", "Cannot reject request in its current status.");
        }
        VarForRejectedRequestId.Status = ServiceRequestStatus.Rejected;
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

        var VarForApprovingCurrentRequest = await _unitOfWork.ServiceRequests.GetByIdAsync(id, cancellationToken);
        

        if (VarForApprovingCurrentRequest is null)
        {
            return ApiResponse<object?>.FailureResponse(
                "Service request was not found.",
                "طلب الخدمة غير موجود.");
        }
        if (currentUser!.EntityType != EntityType.Association || currentUser.EntityId != VarForApprovingCurrentRequest.AssociationId)
                {
                    return ApiResponse<object?>.FailureResponse("ليس لديك الصلاحية للموافقة على طلب الخدمة هذا.", "Only the assigned association may approve this request.");
               }
       if (currentUser!.EntityType != EntityType.Association ||currentUser.EntityId != VarForApprovingCurrentRequest.AssociationId)
        {
            return ApiResponse<object?>.FailureResponse(
                "Only the assigned association may approve and assign workers.",
                "فقط الجمعية التابعة لها الطلب يمكنها الموافقة وتعيين العمال.");
        }
        // Make sure the request is in the correct status
        if (VarForApprovingCurrentRequest.Status != ServiceRequestStatus.PendingWorkerSelection)
        {
            return ApiResponse<object?>.FailureResponse(
                "Cannot approve and assign workers for the request in its current status.",
                "لا يمكن الموافقة وتعيين العمال في حالة الطلب الحالية.");
        }
        if (workerIds is null || workerIds.Count != VarForApprovingCurrentRequest.RequestedWorkersCount)
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

            if (worker.AssociationId != VarForApprovingCurrentRequest.AssociationId)
            {
                return ApiResponse<object?>.FailureResponse(
                    $"Worker {workerId} does not belong to the request association.",
                    $"العامل ذو الرقم {workerId} لا يتبع الجمعية المرتبطة بالطلب.");
            }

            await _unitOfWork.ServiceRequests
            .AssignToServiceRequestAsync(id,workerId,cancellationToken);
        }


        VarForApprovingCurrentRequest.SelectedWorkersCount = workerIds.Count;
        VarForApprovingCurrentRequest.Status = ServiceRequestStatus.InProgress;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object?>.SuccessResponse(null, "تمت الموافقة وتعيين العمال بنجاح.", "Workers assigned successfully.");
    }

}
