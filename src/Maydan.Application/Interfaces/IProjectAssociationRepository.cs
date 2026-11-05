using Maydan.Domain.Entities;

namespace Maydan.Application.Interfaces;

/// <summary>
/// EN: Repository contract for managing ProjectAssociation entities and their nested geographical locations.
/// AR: واجهة المستودع لإدارة كيانات ربط المشاريع بالجمعيات ومواقعها الجغرافية التابعة لها.
/// </summary>
public interface IProjectAssociationRepository
{
    /// <summary>
    /// EN: Retrieves a single ProjectAssociation by ID (read-only) with Project, Association, and Locations loaded.
    /// AR: جلب سجل ربط جمعية بمشروع بواسطة المعرف (للقراءة فقط) مع تضمين بيانات المشروع، الجمعية، والمواقع.
    /// </summary>
    Task<ProjectAssociation?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// EN: Retrieves a single tracked ProjectAssociation by ID for update or delete operations.
    /// AR: جلب سجل ربط جمعية بمشروع (مُتتبع للتعديل أو الحذف) بواسطة المعرف.
    /// </summary>
    Task<ProjectAssociation?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// EN: Retrieves all associations and their locations linked to a specific project.
    /// AR: جلب كافة الجمعيات ومواقعها المرتبطة بمشروع معين.
    /// </summary>
    Task<List<ProjectAssociation>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// EN: Checks if an active link already exists between the given project and association (optionally excluding an ID during updates).
    /// AR: التحقق مما إذا كان هناك ربط فعال مسبق بين المشروع والجمعية (مع إمكانية استثناء معرف معين أثناء التعديل).
    /// </summary>
    Task<bool> ExistsAsync(int projectId, int associationId, int? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// EN: Adds a new ProjectAssociation along with its nested Locations.
    /// AR: إضافة سجل ربط جديد بين مشروع وجمعية مع قائمة المواقع التابعة له.
    /// </summary>
    Task AddAsync(ProjectAssociation projectAssociation, CancellationToken cancellationToken = default);

    /// <summary>
    /// EN: Marks a ProjectAssociation entity for soft-deletion.
    /// AR: حذف سجل ربط الجمعية بالمشروع (حذف منطقي).
    /// </summary>
    void Remove(ProjectAssociation projectAssociation);
}