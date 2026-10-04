using Maydan.Application.DTOs.ServiceRequests;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;

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

    public async Task<AssociationLookupDto> ResolveAssociationByCityIdAsync(int currentUserId, int cityId, CancellationToken cancellationToken = default)
    {
        // authorization: ensure caller is authenticated and has Request Service permission
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken) ?? throw new UnauthorizedAccessException("Current user was not found.");
        if (!currentUser.IsActive)
        {
            throw new UnauthorizedAccessException("Current user is inactive.");
        }

        if (!GetEffectivePermissionIds(currentUser).Contains(RequestServicePermissionId) && !GetEffectivePermissionIds(currentUser).Contains(ManageServiceRequestsPermissionId))
        {
            throw new UnauthorizedAccessException("Caller does not hold Request Service permission.");
        }

        var association = await _unitOfWork.Associations.GetByCityIdAsync(cityId, cancellationToken) 
            ?? throw new KeyNotFoundException("No association found for the specified city.");

        return new AssociationLookupDto
        {
            AssociationId = association.Id,
            EnglishName = association.EnglishName,
            ArabicName = association.ArabicName,
            CityId = association.CityId,
            Latitude = association.Latitude,
            Longitude = association.Longitude
        };
    }

    public async Task<ServiceRequestDto> CreateAsync(int currentUserId, CreateServiceRequestDto dto, CancellationToken cancellationToken = default)
    {
        // Authorization: ensure user exists and holds Request Service permission
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user was not found.");

        if (!currentUser.IsActive)
        {
            throw new UnauthorizedAccessException("Current user is inactive.");
        }

        if (!GetEffectivePermissionIds(currentUser).Contains(RequestServicePermissionId) && !GetEffectivePermissionIds(currentUser).Contains(ManageServiceRequestsPermissionId))
        {
            throw new UnauthorizedAccessException("Caller does not hold Request Service permission.");
        }

        // Validate project and production company membership
        var project = await _unitOfWork.Projects.GetByIdAsync(dto.ProjectId, cancellationToken) ?? throw new KeyNotFoundException("Project was not found.");
        if (currentUser.EntityType != Domain.Enums.EntityType.ProductionCompany || currentUser.EntityId != project.ProductionCompanyId)
        {
            throw new UnauthorizedAccessException("Caller must belong to the production company that owns the project.");
        }

        // Find an association in the requested city (server-side query)
        var association = await _unitOfWork.Associations.GetByCityIdAsync(dto.CityId, cancellationToken) 
            ?? throw new KeyNotFoundException("No association found for the specified city.");

        // Idempotency / duplicate prevention: check via server-side query
        if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
        {
            var existingRequest = await _unitOfWork.ServiceRequests.GetByIdempotencyKeyAsync(dto.IdempotencyKey, project.ProductionCompanyId, cancellationToken);
            if (existingRequest != null)
            {
                throw new InvalidOperationException("Duplicate service request detected.");
            }
        }

        var sr = new ServiceRequest
        {
            ProjectId = dto.ProjectId,
            ProductionCompanyId = project.ProductionCompanyId,
            ServiceId = dto.ServiceId,
            AssociationId = association.Id,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            TimeUnit = dto.TimeUnit,
            ShiftsCount = dto.ShiftsCount,
            // Use provided coordinates if present; otherwise fall back to association coordinates
            //Latitude = dto.Latitude ?? association.Latitude,
            //Longitude = dto.Longitude ?? association.Longitude,
            RequestedWorkersCount = dto.RequestedWorkersCount,
            AttendanceFrequency = dto.AttendanceFrequency,
            AdditionalRequirements = dto.AdditionalRequirements,
            IdempotencyKey = dto.IdempotencyKey,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUserId
        };

        // Snapshot the current service price
        var service = await _unitOfWork.Services.GetByIdAsync(dto.ServiceId, cancellationToken) ?? throw new KeyNotFoundException("Service was not found.");
        sr.UnitPriceSnapshot = service.Price;

        // Calculate expected total amount based on time unit
        sr.ExpectedTotalAmount = dto.TimeUnit switch
        {
            Domain.Enums.ServiceTimeUnit.Shift =>
                // Shift = 12 hours: (12 * service.Price) * requestedWorkers * shiftCount
                (12 * service.Price) * dto.RequestedWorkersCount * dto.ShiftsCount,

            Domain.Enums.ServiceTimeUnit.Day =>
                // Day = 9 hours: (9 * service.Price) * requestedWorkers * dayCount
                (9 * service.Price) * dto.RequestedWorkersCount * dto.ShiftsCount,

            Domain.Enums.ServiceTimeUnit.Hour =>
                // Hour = Direct: (service.Price * hourCount) * requestedWorkers
                (service.Price * dto.ShiftsCount) * dto.RequestedWorkersCount,

            _ => throw new InvalidOperationException("Invalid time unit.")
        };

        await _unitOfWork.ServiceRequests.AddAsync(sr, cancellationToken);
        // SaveChangesAsync will enforce the unique constraint on (IdempotencyKey, ProductionCompanyId).
        // Any violation will be handled by the controller's exception handler.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Immediate notification: send email to association contact and association users
        var assocUsers = await _unitOfWork.Users.GetByEntityAsync(Domain.Enums.EntityType.Association, association.Id, search: null, cancellationToken);
        var prodUsers = await _unitOfWork.Users.GetByEntityAsync(Domain.Enums.EntityType.ProductionCompany, project.ProductionCompanyId, search: null, cancellationToken);

        var subject = "New service request pending worker selection";
        var body = $"A new service request (Id: {sr.Id}) was created for project '{project.ProjectNameEn}' and awaits worker selection.";

        if (!string.IsNullOrWhiteSpace(association.ContactEmail))
        {
            await _emailSender.SendAsync(association.ContactEmail, subject, body, cancellationToken);
        }

        foreach (var u in assocUsers)
        {
            if (!string.IsNullOrWhiteSpace(u.Email))
            {
                await _emailSender.SendAsync(u.Email, subject, body, cancellationToken);
            }
        }

        foreach (var u in prodUsers)
        {
            if (!string.IsNullOrWhiteSpace(u.Email))
            {
                await _emailSender.SendAsync(u.Email, subject, body, cancellationToken);
            }
        }

        return new ServiceRequestDto { Id = sr.Id, Status = sr.Status };
    }

    public async Task<List<ServiceRequestDto>> GetAllAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken) ?? throw new UnauthorizedAccessException("Current user was not found.");
        if (!GetEffectivePermissionIds(currentUser).Contains(ViewServiceRequestsPermissionId) && !GetEffectivePermissionIds(currentUser).Contains(ManageServiceRequestsPermissionId))
        {
            throw new UnauthorizedAccessException("Caller does not hold View Service Requests permission.");
        }

        List<ServiceRequest> results;
        if (currentUser.EntityType == Domain.Enums.EntityType.ProductionCompany)
        {
            results = await _unitOfWork.ServiceRequests.GetByProductionCompanyIdAsync(currentUser.EntityId, cancellationToken);
        }
        else if (currentUser.EntityType == Domain.Enums.EntityType.Association)
        {
            results = await _unitOfWork.ServiceRequests.GetByAssociationIdAsync(currentUser.EntityId, cancellationToken);
        }
        else
        {
            // Fallback: return none
            results = new List<ServiceRequest>();
        }

        return results.Select(r => new ServiceRequestDto { Id = r.Id, Status = r.Status }).ToList();
    }

    public async Task<ServiceRequestDetailsDto> GetByIdAsync(int currentUserId, int id, CancellationToken cancellationToken = default)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken) ?? throw new UnauthorizedAccessException("Current user was not found.");
        if (!GetEffectivePermissionIds(currentUser).Contains(ViewServiceRequestsPermissionId) && !GetEffectivePermissionIds(currentUser).Contains(ManageServiceRequestsPermissionId))
        {
            throw new UnauthorizedAccessException("Caller does not hold View Service Requests permission.");
        }

        var sr = await _unitOfWork.ServiceRequests.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Service request was not found.");

        // Ensure the caller is allowed to view this request: same production company or same association
        if (currentUser.EntityType == Domain.Enums.EntityType.ProductionCompany && currentUser.EntityId != sr.ProductionCompanyId)
        {
            throw new UnauthorizedAccessException("Caller is not authorized to view this service request.");
        }
        if (currentUser.EntityType == Domain.Enums.EntityType.Association && currentUser.EntityId != sr.AssociationId)
        {
            throw new UnauthorizedAccessException("Caller is not authorized to view this service request.");
        }

        // Map to details DTO
        return new ServiceRequestDetailsDto
        {
            Id = sr.Id,
            ServiceId = sr.ServiceId,
            ServiceNameEn = sr.Service?.NameEn ?? string.Empty,
            ServiceNameAr = sr.Service?.NameAr ?? string.Empty,
            ProjectId = sr.ProjectId,
            ProjectNameEn = sr.Project?.ProjectNameEn ?? string.Empty,
            ProjectNameAr = sr.Project?.ProjectNameAr ?? string.Empty,
            AssociationId = sr.AssociationId,
            AssociationNameEn = sr.Association?.EnglishName ?? string.Empty,
            AssociationNameAr = sr.Association?.ArabicName ?? string.Empty,
            CityId = sr.Association?.CityId ?? 0,
            StartDate = sr.StartDate,
            EndDate = sr.EndDate,
            ShiftsCount = sr.ShiftsCount,
            RequestedWorkersCount = sr.RequestedWorkersCount,
            SelectedWorkersCount = sr.SelectedWorkersCount,
            AttendanceFrequency = sr.AttendanceFrequency,
            UnitPriceSnapshot = sr.UnitPriceSnapshot,
            ExpectedTotalAmount = sr.ExpectedTotalAmount,
            Status = sr.Status,
            CreatedAt = sr.CreatedAt,
            CancelledAt = sr.CancelledAt,
            AdditionalRequirements = sr.AdditionalRequirements
        };
    }

    public async Task CancelAsync(int currentUserId, int id, CancellationToken cancellationToken = default)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken) ?? throw new UnauthorizedAccessException("Current user was not found.");

        if (!GetEffectivePermissionIds(currentUser).Contains(RequestServicePermissionId) && !GetEffectivePermissionIds(currentUser).Contains(ManageServiceRequestsPermissionId))
        {
            throw new UnauthorizedAccessException("Caller does not hold Request Service permission.");
        }

        var sr = await _unitOfWork.ServiceRequests.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Service request was not found.");

        // Allow cancellation if caller is admin (ManageServiceRequests) OR the owning production company
        var effective = GetEffectivePermissionIds(currentUser);
        bool isAdmin = effective.Contains(ManageServiceRequestsPermissionId);

        if (!isAdmin)
        {
            if (currentUser.EntityType != Domain.Enums.EntityType.ProductionCompany || currentUser.EntityId != sr.ProductionCompanyId)
            {
                throw new UnauthorizedAccessException("Only the owning production company may cancel this request.");
            }
        }

        if (sr.Status != Domain.Enums.ServiceRequestStatus.PendingWorkerSelection)
        {
            throw new InvalidOperationException("Cannot cancel the request after worker assignment has started.");
        }

        sr.Status = Domain.Enums.ServiceRequestStatus.Cancelled;
        sr.CancelledAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
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

    public async Task<ExpectedPaymentCalculationDto> CalculateExpectedPaymentAsync(int currentUserId, int serviceId, int requestedWorkers, int durationCount, Domain.Enums.ServiceTimeUnit timeUnit, CancellationToken cancellationToken = default)
    {
        // Ensure caller has Request Service permission
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken) ?? throw new UnauthorizedAccessException("Current user was not found.");
        if (!currentUser.IsActive)
        {
            throw new UnauthorizedAccessException("Current user is inactive.");
        }

        if (!GetEffectivePermissionIds(currentUser).Contains(RequestServicePermissionId) && !GetEffectivePermissionIds(currentUser).Contains(ManageServiceRequestsPermissionId))
        {
            throw new UnauthorizedAccessException("Caller does not hold Request Service permission.");
        }

        var service = await _unitOfWork.Services.GetByIdAsync(serviceId, cancellationToken) ?? throw new KeyNotFoundException("Service type not found.");

        // Calculate based on time unit
        decimal totalExpected = timeUnit switch
        {
            Domain.Enums.ServiceTimeUnit.Shift =>
                // Shift = 12 hours: (12 * service.Price) * requestedWorkers * durationCount
                (12 * service.Price) * requestedWorkers * durationCount,

            Domain.Enums.ServiceTimeUnit.Day =>
                // Day = 9 hours: (9 * service.Price) * requestedWorkers * durationCount
                (9 * service.Price) * requestedWorkers * durationCount,

            Domain.Enums.ServiceTimeUnit.Hour =>
                // Hour = Direct: (service.Price * durationCount) * requestedWorkers
                (service.Price * durationCount) * requestedWorkers,

            _ => throw new InvalidOperationException("Invalid time unit.")
        };

        string timeUnitLabel = timeUnit switch
        {
            Domain.Enums.ServiceTimeUnit.Shift => "shifts (12 hours each)",
            Domain.Enums.ServiceTimeUnit.Day => "days (9 hours each)",
            Domain.Enums.ServiceTimeUnit.Hour => "hours",
            _ => "unknown"
        };

        return new ExpectedPaymentCalculationDto
        {
            UnitPrice = service.Price,
            RequestedWorkers = requestedWorkers,
            DurationUnits = durationCount,
            TotalExpectedPayment = totalExpected,
            FormattedMessage = $"Expected payment for {requestedWorkers} workers for {durationCount} {timeUnitLabel}: {totalExpected:N2} JOD."
        };
    }
}
