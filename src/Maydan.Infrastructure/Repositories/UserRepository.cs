using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly MaydanDbContext _context;

    public UserRepository(MaydanDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

    public Task<User?> GetByEmailWithAccessAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Users
            .AsNoTracking()
            .AsSplitQuery()
            .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .Include(u => u.UserGroups)
                .ThenInclude(ug => ug.Group)
                    .ThenInclude(g => g.GroupPermissions)
                        .ThenInclude(gp => gp.Permission)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<List<User>> GetByEntityAsync(EntityType entityType, int entityId, string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .Include(u => u.Role)
            .Where(u => u.EntityType == entityType && u.EntityId == entityId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.FirstNameEn.Contains(term) ||
                u.LastNameEn.Contains(term) ||
                u.FirstNameAr.Contains(term) ||
                u.LastNameAr.Contains(term) ||
                u.Email.Contains(term) ||
                u.PhoneNumber.Contains(term));
        }

        return query
            .OrderBy(u => u.FirstNameEn)
            .ThenBy(u => u.LastNameEn)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<User> Users, int TotalCount)> GetPagedByEntityAsync(
        EntityType entityType, int entityId, string? search, bool? isActive, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.EntityType == entityType && u.EntityId == entityId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.FirstNameEn.Contains(term) ||
                u.LastNameEn.Contains(term) ||
                u.FirstNameAr.Contains(term) ||
                u.LastNameAr.Contains(term) ||
                u.Email.Contains(term) ||
                u.PhoneNumber.Contains(term));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        query = query.OrderBy(u => u.FirstNameEn).ThenBy(u => u.LastNameEn);

        var totalCount = await query.CountAsync(cancellationToken);
        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users, totalCount);
    }

    public Task<User?> GetDetailsAsync(int userId, CancellationToken cancellationToken = default) =>
        _context.Users
            .Include(u => u.Role)
            .Include(u => u.UserGroups)
                .ThenInclude(ug => ug.Group)
                    .ThenInclude(g => g.GroupPermissions)
                        .ThenInclude(gp => gp.Permission)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

    public Task<User?> GetDetailsReadOnlyAsync(int userId, CancellationToken cancellationToken = default) =>
        _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.UserGroups)
                .ThenInclude(ug => ug.Group)
                    .ThenInclude(g => g.GroupPermissions)
                        .ThenInclude(gp => gp.Permission)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByUserNameArAsync(User user, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.FirstNameAr==user.FirstNameAr && u.LastNameAr == user.LastNameAr, cancellationToken);

    public Task<User?> GetByUserNameEnAsync(User user, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.FirstNameEn == user.FirstNameEn && u.LastNameEn == user.LastNameEn, cancellationToken);

    public Task<User?> GetWithPermissionsAsync(int userId, CancellationToken cancellationToken = default) =>
        _context.Users
            .AsNoTracking()
            .AsSplitQuery()
            .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .Include(u => u.UserGroups)
                .ThenInclude(ug => ug.Group)
                    .ThenInclude(g => g.GroupPermissions)
                        .ThenInclude(gp => gp.Permission)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

    public Task<List<User>> GetByIdsInEntityAsync(IEnumerable<int> userIds, EntityType entityType, int entityId, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();

        return _context.Users
            .Where(u => ids.Contains(u.UserId) && u.EntityType == entityType && u.EntityId == entityId)
            .ToListAsync(cancellationToken);
    }

    public Task<List<User>> GetDeletedByEntityAsync(EntityType entityType, int entityId, CancellationToken cancellationToken = default) =>
        _context.Users.IgnoreQueryFilters()
            .Where(u => u.IsDeleted && u.EntityType == entityType && u.EntityId == entityId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await _context.Users.AddAsync(user, cancellationToken);

    public void Remove(User user) => _context.Users.Remove(user);
}
