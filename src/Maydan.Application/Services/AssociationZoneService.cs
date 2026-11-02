using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Common;
using Maydan.Application.Helpers;
using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Common;
using Maydan.Domain.Enums;
using NetTopologySuite.Geometries;

namespace Maydan.Application.Services;

public class AssociationZoneService : IAssociationZoneService
{
    private const int CreateAssociationsPermissionId = 10;
    private const int EditAssociationsPermissionId   = 11;
    private const int DeleteAssociationsPermissionId = 12;
    private const int ManageAssociationsPermissionId = 13;
    private const int ViewAssociationsPermissionId   = 9;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;

    public AssociationZoneService(IUnitOfWork unitOfWork, IEmailSender emailSender)
    {
        _unitOfWork = unitOfWork;
        _emailSender = emailSender;
    }

    // ─── GET ─────────────────────────────────────────────────────────────────

    public async Task<ApiResponse<List<AssociationZoneDto>>> GetByAssociationAsync(
        int currentUserId,
        int associationId,
        CancellationToken cancellationToken = default)
    {
        var authError = await AuthorizeAsync<List<AssociationZoneDto>>(
            currentUserId, ViewAssociationsPermissionId, associationId, cancellationToken);
        if (authError != null) return authError;

        var zones = await _unitOfWork.AssociationZones.GetByAssociationIdAsync(associationId, cancellationToken);
        return ApiResponse<List<AssociationZoneDto>>.SuccessResponse(
            zones.Select(MapToDto).ToList(),
            "Zones retrieved successfully.", "تم جلب المناطق بنجاح.");
    }

    // ─── CREATE ───────────────────────────────────────────────────────────────

    public async Task<ApiResponse<AssociationZoneDto>> CreateAsync(
        int currentUserId,
        int associationId,
        CreateAssociationZoneDto dto,
        CancellationToken cancellationToken = default)
    {
        var authError = await AuthorizeAsync<AssociationZoneDto>(
            currentUserId, EditAssociationsPermissionId, associationId, cancellationToken);
        if (authError != null) return authError;

        var association = await _unitOfWork.Associations.GetByIdAsync(associationId, cancellationToken);
        if (association == null)
            return NotFound<AssociationZoneDto>();

        Polygon boundary;
        try { boundary = GeoJsonHelper.ToPolygon(dto.Boundary); }
        catch (ArgumentException ex)
        { return ApiResponse<AssociationZoneDto>.FailureResponse(ex.Message, ex.Message); }

        // Validate zone lies inside association geofence (if one is defined).
        if (association.Geofence != null && !association.Geofence.Contains(boundary))
            return ApiResponse<AssociationZoneDto>.FailureResponse(
                "Zone boundary must lie entirely inside the association geofence.",
                "يجب أن تقع حدود المنطقة كاملاً داخل السياج الجغرافي للجمعية.");

        var zone = new AssociationZone
        {
            AssociationId = associationId,
            ArabicName    = dto.ArabicName.Trim(),
            EnglishName   = dto.EnglishName.Trim(),
            Boundary      = boundary,
            IsActive      = true,
            CreatedAt     = DateTime.UtcNow
        };

        await _unitOfWork.AssociationZones.AddAsync(zone, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AssociationZoneDto>.SuccessResponse(
            MapToDto(zone), "Zone created successfully.", "تم إنشاء المنطقة بنجاح.", 201);
    }

    // ─── BOOK BY ZONE ID ───────────────────────────────────────────────────────

    public async Task<ApiResponse<AssociationZoneDto>> BookAsync(
        int currentUserId,
        int associationId,
        int zoneId,
        BookZoneDto dto,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken);
        if (currentUser is null || !currentUser.IsActive)
            return new ApiResponse<AssociationZoneDto>(false, "المستخدم غير مصرح له.", "Unauthorized.", default, 401);

        if (dto.StartDate >= dto.EndDate)
            return ApiResponse<AssociationZoneDto>.FailureResponse(
                "EndDate must be after StartDate.", "تاريخ الانتهاء يجب أن يكون بعد تاريخ البداية.");

        var zone = await _unitOfWork.AssociationZones.GetByIdAsync(zoneId, cancellationToken);
        if (zone == null || zone.AssociationId != associationId)
            return NotFound<AssociationZoneDto>();

        // Check overlap with existing booking.
        if (zone.BookedFrom.HasValue && zone.BookedUntil.HasValue)
        {
            bool overlaps = dto.StartDate.ToUniversalTime() < zone.BookedUntil.Value && dto.EndDate.ToUniversalTime() > zone.BookedFrom.Value;
            if (overlaps)
                return ApiResponse<AssociationZoneDto>.FailureResponse(
                    "Zone is already booked during the requested period.",
                    $"المنطقة محجوزة مسبقاً من {zone.BookedFrom:d} حتى {zone.BookedUntil:d}.");
        }

        zone.BookedFrom  = dto.StartDate.ToUniversalTime();
        zone.BookedUntil = dto.EndDate.ToUniversalTime();
        zone.UpdatedAt   = DateTime.UtcNow;

        // Auto-create ServiceRequest if requested
        if (dto.AutoCreateServiceRequest && dto.ProjectId.HasValue && dto.ServiceId.HasValue)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(dto.ProjectId.Value, cancellationToken);
            var service = await _unitOfWork.Services.GetByIdAsync(dto.ServiceId.Value, cancellationToken);

            if (project != null && service != null)
            {
                var shifts = dto.ShiftsCount.GetValueOrDefault(1);
                var workers = dto.RequestedWorkersCount.GetValueOrDefault(1);
                var unitPrice = service.Price;
                var total = unitPrice * shifts * workers;

                var sr = new ServiceRequest
                {
                    ProjectId = project.Id,
                    ProductionCompanyId = project.ProductionCompanyId,
                    ServiceId = service.Id,
                    AssociationId = associationId,
                    StartDate = zone.BookedFrom.Value,
                    EndDate = zone.BookedUntil.Value,
                    ShiftsCount = shifts,
                    RequestedWorkersCount = workers,
                    AttendanceFrequency = dto.AttendanceFrequency.GetValueOrDefault(1),
                    AdditionalRequirements = string.IsNullOrWhiteSpace(dto.AdditionalRequirements)
                        ? $"حجز تلقائي للقطعة: {zone.ArabicName}"
                        : dto.AdditionalRequirements,
                    UnitPriceSnapshot = unitPrice,
                    ExpectedTotalAmount = total,
                    StatusId = LookupConstants.ServiceRequestStatus.PendingWorkerSelection,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId,
                    IsActive = true
                };

                await _unitOfWork.ServiceRequests.AddAsync(sr, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                zone.ServiceRequestId = sr.Id;
            }
        }
        else if (dto.ServiceRequestId.HasValue)
        {
            zone.ServiceRequestId = dto.ServiceRequestId;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Fire-and-forget email notifications (non-blocking).
        _ = SendBookingNotificationsAsync(zone, currentUser, cancellationToken);

        return ApiResponse<AssociationZoneDto>.SuccessResponse(
            MapToDto(zone), "Zone booked successfully.", "تم حجز المنطقة بنجاح.");
    }

    // ─── BOOK BY LOCATION (TAP ON MAP) ────────────────────────────────────────

    public async Task<ApiResponse<AssociationZoneDto>> BookByLocationAsync(
        int currentUserId,
        BookByLocationDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.StartDate >= dto.EndDate)
            return ApiResponse<AssociationZoneDto>.FailureResponse(
                "EndDate must be after StartDate.", "تاريخ الانتهاء يجب أن يكون بعد تاريخ البداية.");

        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        // Note: NetTopologySuite Coordinate is (longitude, latitude) -> (X, Y)
        var point = geometryFactory.CreatePoint(new Coordinate(dto.Longitude, dto.Latitude));

        var matchedZone = await _unitOfWork.AssociationZones
            .FindAvailableContainingAsync(point, dto.StartDate.ToUniversalTime(), dto.EndDate.ToUniversalTime(), cancellationToken);

        if (matchedZone == null)
        {
            return ApiResponse<AssociationZoneDto>.FailureResponse(
                "No available bookable zone found at this location for the requested period.",
                "هذه النقطة لا تقع داخل أي منطقة متاحة للحجز حالياً.");
        }

        var bookDto = new BookZoneDto
        {
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            AutoCreateServiceRequest = dto.AutoCreateServiceRequest,
            ProjectId = dto.ProjectId,
            ServiceId = dto.ServiceId,
            RequestedWorkersCount = dto.RequestedWorkersCount,
            ShiftsCount = dto.ShiftsCount,
            AttendanceFrequency = dto.AttendanceFrequency,
            AdditionalRequirements = dto.AdditionalRequirements
        };

        return await BookAsync(currentUserId, matchedZone.AssociationId, matchedZone.Id, bookDto, cancellationToken);
    }

    // ─── DELETE ───────────────────────────────────────────────────────────────

    public async Task<ApiResponse<object?>> DeleteAsync(
        int currentUserId,
        int associationId,
        int zoneId,
        CancellationToken cancellationToken = default)
    {
        var authError = await AuthorizeAsync<object?>(
            currentUserId, DeleteAssociationsPermissionId, associationId, cancellationToken);
        if (authError != null) return authError;

        var zone = await _unitOfWork.AssociationZones.GetByIdAsync(zoneId, cancellationToken);
        if (zone == null || zone.AssociationId != associationId)
            return NotFound<object?>();

        _unitOfWork.AssociationZones.Remove(zone);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object?>.SuccessResponse(null, "Zone deleted successfully.", "تم حذف المنطقة بنجاح.");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<ApiResponse<T>?> AuthorizeAsync<T>(
        int currentUserId,
        int requiredPermissionId,
        int associationId,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetWithPermissionsAsync(currentUserId, cancellationToken);
        if (user is null || !user.IsActive)
            return new ApiResponse<T>(false, "المستخدم غير مصرح له.", "Unauthorized.", default, 401);

        var permIds = GetEffectivePermissionIds(user);
        if (!permIds.Overlaps(new[] { requiredPermissionId, ManageAssociationsPermissionId }))
            return new ApiResponse<T>(false, "لا تملك الصلاحية المطلوبة.", "Insufficient permissions.", default, 403);

        if (user.EntityType == EntityType.Association && user.EntityId != associationId)
            return new ApiResponse<T>(false, "لا تملك صلاحية الوصول لمنطقة تابعة لجمعية أخرى.",
                "Cannot access zones outside your own association.", default, 403);

        return null;
    }

    private async Task SendBookingNotificationsAsync(AssociationZone zone, User bookingUser, CancellationToken cancellationToken)
    {
        try
        {
            var association = zone.Association
                ?? await _unitOfWork.Associations.GetByIdAsync(zone.AssociationId, cancellationToken);

            if (association == null) return;

            var subject = $"تأكيد حجز منطقة: {zone.ArabicName} | Zone Booking Confirmed: {zone.EnglishName}";
            var body = $"تم تأكيد حجز المنطقة \"{zone.ArabicName} ({zone.EnglishName})\" " +
                       $"من {zone.BookedFrom:yyyy-MM-dd} إلى {zone.BookedUntil:yyyy-MM-dd}.\n" +
                       (zone.ServiceRequestId.HasValue ? $"تم إنشاء طلب خدمة مرتبط رقم: #{zone.ServiceRequestId.Value}" : "");

            // 1. Notify association contact email
            if (!string.IsNullOrWhiteSpace(association.ContactEmail))
                await _emailSender.SendAsync(association.ContactEmail, subject, body, cancellationToken);

            // 2. Notify all association users
            var assocUsers = await _unitOfWork.Users.GetByEntityAsync(
                EntityType.Association, association.Id, null, cancellationToken);

            foreach (var u in assocUsers.Where(u => !string.IsNullOrWhiteSpace(u.Email)))
                await _emailSender.SendAsync(u.Email!, subject, body, cancellationToken);

            // 3. Notify the booking user (e.g. Production Company)
            if (!string.IsNullOrWhiteSpace(bookingUser.Email))
                await _emailSender.SendAsync(bookingUser.Email!, subject, body, cancellationToken);
        }
        catch
        {
            // Non-critical background task — swallow to prevent failure propagation.
        }
    }

    private static AssociationZoneDto MapToDto(AssociationZone zone) => new()
    {
        Id                     = zone.Id,
        AssociationId          = zone.AssociationId,
        AssociationArabicName  = zone.Association?.ArabicName,
        AssociationEnglishName = zone.Association?.EnglishName,
        ArabicName             = zone.ArabicName,
        EnglishName            = zone.EnglishName,
        Boundary               = GeoJsonHelper.FromPolygon(zone.Boundary),
        IsBookedNow            = zone.IsBookedNow,
        BookedFrom             = zone.BookedFrom,
        BookedUntil            = zone.BookedUntil,
        ServiceRequestId       = zone.ServiceRequestId
    };

    private static ApiResponse<T> NotFound<T>() =>
        new(false, "المنطقة غير موجودة.", "Zone was not found.", default, 404);

    private static HashSet<int> GetEffectivePermissionIds(Domain.Entities.User user)
    {
        var roleIds = user.Role?.RolePermissions?
            .Where(rp => rp.IsActive && (rp.Permission == null || rp.Permission.IsActive))
            .Select(rp => rp.PermissionId) ?? Enumerable.Empty<int>();

        var directIds = user.UserPermissions?
            .Where(up => up.IsActive && (up.Permission == null || up.Permission.IsActive))
            .Select(up => up.PermissionId) ?? Enumerable.Empty<int>();

        var groupIds = user.UserGroups?
            .Where(ug => ug.Group != null && ug.Group.IsActive)
            .SelectMany(ug => ug.Group.GroupPermissions ?? new List<Domain.Entities.GroupPermission>())
            .Where(gp => gp.IsActive && (gp.Permission == null || gp.Permission.IsActive))
            .Select(gp => gp.PermissionId) ?? Enumerable.Empty<int>();

        return roleIds.Concat(directIds).Concat(groupIds).ToHashSet();
    }
}
