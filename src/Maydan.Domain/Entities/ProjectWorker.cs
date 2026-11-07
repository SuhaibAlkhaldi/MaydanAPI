using Maydan.Domain.Common;

namespace Maydan.Domain.Entities;

/// <summary>
/// EN: Links a Worker directly to a Project with a specified daily wage and operational status.
/// AR: يربط العامل بالمشروع بشكل مباشر مع تحديد الأجر اليومي وحالة تفعيل العامل في المشروع.
/// </summary>
public class ProjectWorker : SharedEntities
{
    // EN: Foreign key referencing the Project.
    // AR: المفتاح الأجنبي الذي يشير إلى المشروع.
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    // EN: Foreign key referencing the Worker.
    // AR: المفتاح الأجنبي الذي يشير إلى العامل.
    public int WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    // EN: Daily wage amount allocated to the worker for this specific project.
    // AR: قيمة الأجر اليومي المخصص للعامل في هذا المشروع تحديداً.
    public decimal DailyWageAmount { get; set; }

    // EN: Indicates whether the worker is currently active on this project.
    // AR: يحدد ما إذا كان العامل نشطاً حالياً في هذا المشروع أم موقوفاً.
    public bool IsWorkerActive { get; set; } = true;
}