namespace Maydan.Application.DTOs.Projects;

/// <summary>
/// EN: Data Transfer Object representing a geographical location linked to a project association.
/// AR: كائن نقل البيانات الذي يمثل موقعاً جغرافياً مرتبطاً بجمعية داخل المشروع.
/// </summary>
public class ProjectAssociationLocationDto
{
    public int Id { get; set; }
    public int ProjectAssociationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LocationOnGoogleMaps { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
}

/// <summary>
/// EN: Payload for creating or updating a location within a ProjectAssociation request.
/// AR: نموذج البيانات المرسل عند إنشاء أو تحديث موقع جغرافي تابع لجمعية في المشروع.
/// </summary>
public class CreateUpdateProjectAssociationLocationDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LocationOnGoogleMaps { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
}

/// <summary>
/// EN: Enriched response DTO for a ProjectAssociation including project names, association names, and locations.
/// AR: نموذج الاستجابة الشامل لربط المشروع بالجمعية متضمناً أسماء المشروع والجمعية وقائمة المواقع.
/// </summary>
public class ProjectAssociationDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectNameAr { get; set; } = string.Empty;
    public string ProjectNameEn { get; set; } = string.Empty;
    public int AssociationId { get; set; }
    public string AssociationNameAr { get; set; } = string.Empty;
    public string AssociationNameEn { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public List<ProjectAssociationLocationDto> Locations { get; set; } = new();
}

/// <summary>
/// EN: Request payload for linking an association and its locations to a project.
/// AR: نموذج الطلب لربط جمعية ومواقعها الجغرافية بمشروع محدد.
/// </summary>
public class CreateProjectAssociationDto
{
    public int ProjectId { get; set; }
    public int AssociationId { get; set; }
    public List<CreateUpdateProjectAssociationLocationDto> Locations { get; set; } = new();
}

/// <summary>
/// EN: Request payload for updating an existing ProjectAssociation and replacing its locations.
/// AR: نموذج الطلب لتحديث ربط جمعية بمشروع وتحديث قائمة المواقع الخاصة بها.
/// </summary>
public class UpdateProjectAssociationDto
{
    public int AssociationId { get; set; }
    public List<CreateUpdateProjectAssociationLocationDto> Locations { get; set; } = new();
}