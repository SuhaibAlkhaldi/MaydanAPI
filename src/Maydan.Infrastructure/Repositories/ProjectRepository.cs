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
            .Include(x => x.City)
            .Include(x => x.CityLocation)
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
            .Include(x => x.City)
            .Include(x => x.CityLocation)
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
            .Include(x => x.City)
            .Include(x => x.CityLocation)
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
        int? cityId,
        int? cityLocationId,
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
            .Include(x => x.ProductionCompany)
            .Include(x => x.City)
            .Include(x => x.CityLocation);

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
        if (cityId.HasValue && cityId.Value > 0)
        {
            query = query.Where(x => x.CityId == cityId.Value);
        }

        if (cityLocationId.HasValue && cityLocationId.Value > 0)
        {
            query = query.Where(x => x.CityLocationId == cityLocationId.Value);
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



    public async Task<string?> CheckOverlapConflictsAsync(
    DateTime startDate,
    DateTime endDate,
    int producerId,
    int locationManagerId,
    int cityLocationId,
    int? excludeProjectId = null,
    CancellationToken cancellationToken = default)
    {
     
        var query = _context.Projects
            .Where(p => !p.IsDeleted && p.IsActive)
            .Where(p => p.StartDate <= endDate && p.EndDate >= startDate);


        if (excludeProjectId.HasValue)
        {
            query = query.Where(p => p.Id != excludeProjectId.Value);
        }


        var overlaps = await query
            .Select(p => new { p.ProducerUserId, p.LocationManagerUserId, p.CityLocationId })
            .ToListAsync(cancellationToken);

        if (!overlaps.Any()) return null; 


        if (overlaps.Any(p => p.ProducerUserId == producerId || p.LocationManagerUserId == producerId))
        {
            return "المنتج المختار مرتبط بمشروع آخر في نفس الفترة الزمنية.";
        }


        if (overlaps.Any(p => p.ProducerUserId == locationManagerId || p.LocationManagerUserId == locationManagerId))
        {
            return "مدير الموقع المختار مرتبط بمشروع آخر في نفس الفترة الزمنية.";
        }


        if (overlaps.Any(p => p.CityLocationId == cityLocationId))
        {
            return "يوجد مشروع آخر قيد التنفيذ في نفس الموقع التفصيلي خلال هذه الفترة الزمنية.";
        }

        return null; 
    }

    public async Task<List<int>> GetBusyUserIdsAsync(
    int productionCompanyId,
    DateTime startDate,
    DateTime endDate,
    int? excludeProjectId = null,
    CancellationToken cancellationToken = default)
    {

        var query = _context.Projects
            .Where(p => !p.IsDeleted && p.IsActive)
            .Where(p => p.ProductionCompanyId == productionCompanyId)
            .Where(p => p.StartDate <= endDate && p.EndDate >= startDate);


        if (excludeProjectId.HasValue)
        {
            query = query.Where(p => p.Id != excludeProjectId.Value);
        }


        var busyProducers = await query.Select(p => p.ProducerUserId).ToListAsync(cancellationToken);
        var busyLocationManagers = await query.Select(p => p.LocationManagerUserId).ToListAsync(cancellationToken);


        return busyProducers.Concat(busyLocationManagers).Distinct().ToList();
    }
}
