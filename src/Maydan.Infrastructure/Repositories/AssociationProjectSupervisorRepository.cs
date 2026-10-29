using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

public class AssociationProjectSupervisorRepository : IAssociationProjectSupervisorRepository
{
    private readonly MaydanDbContext _context;

    public AssociationProjectSupervisorRepository(MaydanDbContext context)
    {
        _context = context;
    }

    public async Task<(List<AssociationProjectSupervisor> Items, int TotalCount)> GetAllAsync(
     int? projectId, int? associationId, int? productionCompanyId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.AssociationProjectSupervisors
            .Include(s => s.Project)
            .Include(s => s.User)
            .AsQueryable();

        if (projectId is not null)
        {
            query = query.Where(s => s.ProjectId == projectId);
        }

        if (associationId is not null)
        {
            query = query.Where(s => s.User.EntityType == EntityType.Association && s.User.EntityId == associationId);
        }
        if (productionCompanyId is not null)
            query = query.Where(s => s.Project != null && s.Project.ProductionCompanyId == productionCompanyId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<List<AssociationProjectSupervisor>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default) =>
        _context.AssociationProjectSupervisors
            .Include(s => s.Project)
            .Include(s => s.User)
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<AssociationProjectSupervisor?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.AssociationProjectSupervisors
            .Include(s => s.Project)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task AddAsync(AssociationProjectSupervisor supervisor, CancellationToken cancellationToken = default) =>
        await _context.AssociationProjectSupervisors.AddAsync(supervisor, cancellationToken);

    public void Remove(AssociationProjectSupervisor supervisor) => _context.AssociationProjectSupervisors.Remove(supervisor);
}
