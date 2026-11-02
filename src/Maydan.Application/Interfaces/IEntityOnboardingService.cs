using Maydan.Application.DTOs.Common;
using Maydan.Application.DTOs.Onboarding;
using Maydan.Application.DTOs.UserManagement;

namespace Maydan.Application.Interfaces;

public interface IEntityOnboardingService
{
    Task<ApiResponse<UserDetailsDto>> OnboardAssociationAdminAsync(int currentUserId, int associationId, OnboardAssociationAdminDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<AssociationWithoutAdminDto>>> GetAssociationsWithoutAdminAsync(int currentUserId, CancellationToken cancellationToken = default);
}