using Maydan.Application.DTOs.Associations;
using Maydan.Application.DTOs.Common;

namespace Maydan.Application.Interfaces;

public interface IAssociationZoneService
{
    /// <summary>GET api/associations/{associationId}/zones</summary>
    Task<ApiResponse<List<AssociationZoneDto>>> GetByAssociationAsync(int currentUserId, int associationId, CancellationToken cancellationToken = default);

    /// <summary>POST api/associations/{associationId}/zones</summary>
    Task<ApiResponse<AssociationZoneDto>> CreateAsync(int currentUserId, int associationId, CreateAssociationZoneDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// POST api/associations/{associationId}/zones/{zoneId}/book
    /// Marks zone as booked for the given window, optionally creates a ServiceRequest, and fires notifications.
    /// </summary>
    Task<ApiResponse<AssociationZoneDto>> BookAsync(int currentUserId, int associationId, int zoneId, BookZoneDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// POST api/associations/zones/book-by-location
    /// Finds the zone containing the specified (lat, lng) point and books it.
    /// </summary>
    Task<ApiResponse<AssociationZoneDto>> BookByLocationAsync(int currentUserId, BookByLocationDto dto, CancellationToken cancellationToken = default);

    /// <summary>DELETE api/associations/{associationId}/zones/{zoneId}</summary>
    Task<ApiResponse<object?>> DeleteAsync(int currentUserId, int associationId, int zoneId, CancellationToken cancellationToken = default);
}
