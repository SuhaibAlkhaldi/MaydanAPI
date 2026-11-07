using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

/// <summary>
/// EN: EF Core implementation of IProjectAssociationRepository using optimized split queries and no-tracking reads.
/// AR: التنفيذ الفعلي لمستودع جمعيات المشروع باستخدام استعلامات مقسمة ومحسنة للأداء.
/// </summary>
public class ProjectAssociationRepository : IProjectAssociationRepository
{
    private readonly MaydanDbContext _context;

    public ProjectAssociationRepository(MaydanDbContext context)
    {
        _context = context;
    }

    public Task<ProjectAssociation?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.ProjectAssociations
            .AsNoTracking()
            .AsSplitQuery() // EN: Prevents Cartesian explosion when loading one-to-many Locations / AR: يمنع تضاعف الصفوف عند جلب قائمة المواقع
            .Include(pa => pa.Project)
            .Include(pa => pa.Association)
            .Include(pa => pa.Locations.OrderBy(l => l.Id))
            .FirstOrDefaultAsync(pa => pa.Id == id, cancellationToken);

    public Task<ProjectAssociation?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        _context.ProjectAssociations
            .Include(pa => pa.Project)
            .Include(pa => pa.Association)
            .Include(pa => pa.Locations)
            .FirstOrDefaultAsync(pa => pa.Id == id, cancellationToken);

    public Task<List<ProjectAssociation>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default) =>
        _context.ProjectAssociations
            .AsNoTracking()
            .AsSplitQuery()
            .Include(pa => pa.Project)
            .Include(pa => pa.Association)
            .Include(pa => pa.Locations.OrderBy(l => l.Id))
            .Where(pa => pa.ProjectId == projectId)
            .OrderByDescending(pa => pa.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(int projectId, int associationId, int? excludeId = null, CancellationToken cancellationToken = default) =>
        _context.ProjectAssociations
            .AsNoTracking()
            .AnyAsync(pa => pa.ProjectId == projectId
                         && pa.AssociationId == associationId
                         && (!excludeId.HasValue || pa.Id != excludeId.Value),
                      cancellationToken);

    public async Task AddAsync(ProjectAssociation projectAssociation, CancellationToken cancellationToken = default) =>
        await _context.ProjectAssociations.AddAsync(projectAssociation, cancellationToken);

    public void Remove(ProjectAssociation projectAssociation) =>
        _context.ProjectAssociations.Remove(projectAssociation);
}