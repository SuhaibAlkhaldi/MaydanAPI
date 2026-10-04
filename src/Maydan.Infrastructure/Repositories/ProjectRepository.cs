using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly MaydanDbContext _context;

    public ProjectRepository(MaydanDbContext context)
    {
        _context = context;
    }

    public Task<Project?> GetByIdAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        return _context.Projects
            .Include(x => x.ProjectType)
            .Include(x => x.Producer)
            .Include(x => x.LocationManager)
            .Include(x => x.ProductionCompany)
            .FirstOrDefaultAsync(
                x => x.Id == projectId,
                cancellationToken);
    }

    public Task<Project?> GetByIdIncludingDeletedAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        return _context.Projects
            .IgnoreQueryFilters()
            .Include(x => x.ProjectType)
            .Include(x => x.Producer)
            .Include(x => x.LocationManager)
            .Include(x => x.ProductionCompany)
            .FirstOrDefaultAsync(
                x => x.Id == projectId,
                cancellationToken);
    }

    public Task<List<Project>> GetByProductionCompanyIdAsync(
        int productionCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _context.Projects
            .AsNoTracking()
            .Include(x => x.ProjectType)
            .Include(x => x.Producer)
            .Include(x => x.LocationManager)
            .Where(x => x.ProductionCompanyId == productionCompanyId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Project>> GetAllAsync(
        bool isDeleted,
        string? searchTerm,
        DateTime? startDate,
        DateTime? endDate,
        string? searchByProductionCompanyName,
        int? searchByProductionCompanyId,
        CancellationToken cancellationToken = default)
    {

        // The global soft-delete filter already excludes IsDeleted rows for the normal (active)
        // view, so only the deleted view needs to explicitly ignore it.
        IQueryable<Project> query = isDeleted
            ? _context.Projects.IgnoreQueryFilters().Where(x => x.IsDeleted)
            : _context.Projects;

        query = query
            .AsNoTracking()
            .Include(x => x.ProjectType)
            .Include(x => x.Producer)
            .Include(x => x.LocationManager)
            .Include(x => x.ProductionCompany);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(x =>
                x.ProjectNameEn.Contains(term) ||
                x.ProjectNameAr.Contains(term));
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.StartDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.EndDate <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchByProductionCompanyName))
        {
            var name = searchByProductionCompanyName.Trim();
            query = query.Where(x =>
                x.ProductionCompany.EnglishName.Contains(name) ||
                x.ProductionCompany.ArabicName.Contains(name));
        }

        if (searchByProductionCompanyId.HasValue)
        {
            query = query.Where(x => x.ProductionCompanyId == searchByProductionCompanyId.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Project project,
        CancellationToken cancellationToken = default)
    {
        await _context.Projects.AddAsync(project, cancellationToken);
    }

    public void Remove(Project project)
    {
        _context.Projects.Remove(project);
    }
}
