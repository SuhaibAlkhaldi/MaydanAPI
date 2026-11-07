using Maydan.Application.Interfaces;
using Maydan.Domain.Entities;
using Maydan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maydan.Infrastructure.Repositories;

/// <summary>
/// EN: EF Core implementation of IProjectWorkerRepository with server-side dynamic filtering.
/// AR: التنفيذ الفعلي لمستودع عمال المشروع مع دعم الفلترة الديناميكية المباشرة على مستوى قاعدة البيانات.
/// </summary>
public class ProjectWorkerRepository : IProjectWorkerRepository
{
    private readonly MaydanDbContext _context;

    public ProjectWorkerRepository(MaydanDbContext context)
    {
        _context = context;
    }

    public Task<ProjectWorker?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.ProjectWorkers
            .AsNoTracking()
            .Include(pw => pw.Project)
            .Include(pw => pw.Worker)
            .FirstOrDefaultAsync(pw => pw.Id == id, cancellationToken);

    public Task<ProjectWorker?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        _context.ProjectWorkers
            .Include(pw => pw.Project)
            .Include(pw => pw.Worker)
            .FirstOrDefaultAsync(pw => pw.Id == id, cancellationToken);

    public Task<List<ProjectWorker>> GetByProjectIdAsync(
        int projectId,
        string? searchByWorkerName = null,
        string? searchByWorkerCivilianId = null,
        bool? isWorkerActive = null,
        CancellationToken cancellationToken = default)
    {
        // EN: Start with base query filtered by ProjectId and eager-load Worker & Project.
        // AR: البدء بالاستعلام الأساسي المفلتر برقم المشروع مع تضمين بيانات العامل والمشروع.
        var query = _context.ProjectWorkers
            .AsNoTracking()
            .Include(pw => pw.Project)
            .Include(pw => pw.Worker)
            .Where(pw => pw.ProjectId == projectId)
            .AsQueryable();

        // EN: 1. Filter by Worker Active Status if provided.
        // AR: ١. الفلترة حسب حالة تفعيل العامل في حال تم إرسالها.
        if (isWorkerActive.HasValue)
        {
            query = query.Where(pw => pw.IsWorkerActive == isWorkerActive.Value);
        }

        // EN: 2. Filter by Worker National/Civil ID if provided.
        // AR: ٢. الفلترة حسب الرقم الوطني للعامل في حال تم إرساله.
        if (!string.IsNullOrWhiteSpace(searchByWorkerCivilianId))
        {
            var cleanCivilId = searchByWorkerCivilianId.Trim();
            query = query.Where(pw => pw.Worker.CivilId.Contains(cleanCivilId));
        }

        // EN: 3. Filter by Worker Name (supports searching First, Middle, Last, or combined Full Name in SQL).
        // AR: ٣. الفلترة حسب اسم العامل (يدعم البحث بالاسم الأول، الأوسط، الأخير، أو الاسم الكامل المركب).
        if (!string.IsNullOrWhiteSpace(searchByWorkerName))
        {
            var cleanName = searchByWorkerName.Trim();
            query = query.Where(pw =>
                pw.Worker.FirstName.Contains(cleanName) ||
                (pw.Worker.MiddleName != null && pw.Worker.MiddleName.Contains(cleanName)) ||
                pw.Worker.LastName.Contains(cleanName) ||
                (pw.Worker.FirstName + " " + pw.Worker.LastName).Contains(cleanName) ||
                (pw.Worker.FirstName + " " + (pw.Worker.MiddleName ?? "") + " " + pw.Worker.LastName).Contains(cleanName));
        }

        return query
            .OrderByDescending(pw => pw.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(int projectId, int workerId, CancellationToken cancellationToken = default) =>
        _context.ProjectWorkers
            .AsNoTracking()
            .AnyAsync(pw => pw.ProjectId == projectId && pw.WorkerId == workerId, cancellationToken);

    public async Task AddAsync(ProjectWorker projectWorker, CancellationToken cancellationToken = default) =>
        await _context.ProjectWorkers.AddAsync(projectWorker, cancellationToken);
}