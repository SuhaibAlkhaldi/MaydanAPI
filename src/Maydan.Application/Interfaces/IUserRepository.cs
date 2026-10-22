using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailWithAccessAsync(string email, CancellationToken cancellationToken = default);
    Task<List<User>> GetByEntityAsync(EntityType entityType, int entityId, string? search, CancellationToken cancellationToken = default);

    // MAYD-20: real server-side pagination + status filter, on top of the same search behavior as
    // GetByEntityAsync above (kept as its own method rather than adding optional params to that
    // one — GetByEntityAsync has other real callers, e.g. UserManagementService.CreateUserAsync's
    // duplicate-name checks via GetByUserNameEnAsync/GetByUserNameArAsync elsewhere, that have no
    // use for paging/status and shouldn't need to pass through defaults for it).
    Task<(List<User> Users, int TotalCount)> GetPagedByEntityAsync(
        EntityType entityType, int entityId, string? search, bool? isActive, int page, int pageSize,
        CancellationToken cancellationToken = default);
    Task<User?> GetDetailsAsync(int userId, CancellationToken cancellationToken = default);
    Task<User?> GetDetailsReadOnlyAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> PhoneExistsAsync(
     string phoneNumber,
     CancellationToken cancellationToken = default);
    Task<User?> GetByUserNameEnAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetByUserNameArAsync(User user, CancellationToken cancellationToken = default);

    // Loads Role, Groups and direct UserPermissions so the effective-permissions union (section 4) can be computed.
    Task<User?> GetWithPermissionsAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<User>> GetByIdsInEntityAsync(IEnumerable<int> userIds, EntityType entityType, int entityId, CancellationToken cancellationToken = default);

    // MAYD-51 (Association Management, Phase 2c): the mirror image of GetByEntityAsync above —
    // SOFT-DELETED rows only (IgnoreQueryFilters + an explicit IsDeleted check), not the active-only
    // default every other caller of GetByEntityAsync relies on. Needed by
    // AssociationService.RestoreAsync's own cascade to find the real Users a cascade-delete touched,
    // so it can compare each one's DeletedAt against the Association's own and restore only the ones
    // deleted in that SAME cascade (see that method's own comment) — a User independently deleted
    // for an unrelated reason must not be silently revived.
    Task<List<User>> GetDeletedByEntityAsync(EntityType entityType, int entityId, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Remove(User user);
}
