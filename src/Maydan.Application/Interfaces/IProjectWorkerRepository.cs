using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

/// <summary>
/// EN: Repository contract for managing ProjectWorker assignments, filtering, and status toggling.
/// AR: واجهة المستودع لإدارة تعيينات عمال المشروع، الفلترة، وتغيير حالة التفعيل.
/// </summary>
public interface IProjectWorkerRepository
{
    /// <summary>
    /// EN: Retrieves a single ProjectWorker by ID (read-only) with Worker and Project details loaded.
    /// AR: جلب سجل عامل في مشروع بواسطة المعرف (للقراءة فقط) مع تضمين بيانات العامل والمشروع.
    /// </summary>
    Task<ProjectWorker?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// EN: Retrieves a single tracked ProjectWorker by ID for update, activation, or deactivation.
    /// AR: جلب سجل عامل في مشروع (مُتتبع للتعديل أو التفعيل/التعطيل) بواسطة المعرف.
    /// </summary>
    Task<ProjectWorker?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// EN: Retrieves all workers assigned to a specific project with optional server-side filters for name, civil ID, and active status.
    /// AR: جلب جميع العمال المعينين في مشروع معين مع فلاتر اختيارية (الاسم، الرقم الوطني، وحالة التفعيل).
    /// </summary>
    Task<List<ProjectWorker>> GetByProjectIdAsync(
        int projectId,
        string? searchByWorkerName = null,
        string? searchByWorkerCivilianId = null,
        bool? isWorkerActive = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// EN: Checks if a worker is already assigned to the specified project.
    /// AR: التحقق مما إذا كان العامل معيناً مسبقاً في نفس المشروع.
    /// </summary>
    Task<bool> ExistsAsync(int projectId, int workerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// EN: Assigns a new worker to a project.
    /// AR: إضافة تعيين جديد لعامل في مشروع.
    /// </summary>
    Task AddAsync(ProjectWorker projectWorker, CancellationToken cancellationToken = default);
}