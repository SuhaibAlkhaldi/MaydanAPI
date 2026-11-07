namespace Maydan.Application.DTOs.Projects;

/// <summary>
/// EN: Response DTO representing a worker assigned to a project with full details and daily wage.
/// AR: نموذج الاستجابة الذي يمثل عاملاً معيناً في مشروع مع كامل بياناته وأجره اليومي.
/// </summary>
public class ProjectWorkerDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectNameAr { get; set; } = string.Empty;
    public string ProjectNameEn { get; set; } = string.Empty;
    public int WorkerId { get; set; }
    public string WorkerName { get; set; } = string.Empty;

    // EN: Matches frontend typo 'workerCivilainId' to prevent undefined binding in Angular tables.
    // AR: يطابق التسمية المتوقعة في الفرونت إند لضمان ظهور الرقم الوطني في الجدول بدون مشاكل.
    public string WorkerCivilainId { get; set; } = string.Empty;
    public string WorkerCivilId { get; set; } = string.Empty;
    public string? WorkerPhoneNumber { get; set; }
    public decimal DailyWageAmount { get; set; }
    public bool IsWorkerActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}

/// <summary>
/// EN: Request payload for assigning a worker to a project.
/// AR: نموذج الطلب لتعيين عامل جديد في مشروع وتحديد أجره اليومي.
/// </summary>
public class CreateProjectWorkerDto
{
    public int ProjectId { get; set; }
    public int WorkerId { get; set; }
    public decimal DailyWageAmount { get; set; }
}

/// <summary>
/// EN: Request payload for updating a project worker's assignment details and daily wage.
/// AR: نموذج الطلب لتعديل بيانات تعيين العامل في المشروع وأجره اليومي.
/// </summary>
public class UpdateProjectWorkerDto
{
    public int WorkerId { get; set; }
    public decimal DailyWageAmount { get; set; }
    public bool IsWorkerActive { get; set; } = true;
}