using Maydan.Domain.Common;

namespace Maydan.Domain.Entities;

/// <summary>
/// EN: Junction entity linking a Project to an Association, holding a one-to-many collection of specific operational locations.
/// AR: كيان ربط يجمع بين المشروع والجمعية، ويحتوي على علاقة (واحد لمتعدد) مع المواقع الجغرافية الخاصة بهذه الجمعية ضمن المشروع.
/// </summary>
public class ProjectAssociation : SharedEntities
{
    // EN: Foreign key referencing the Project.
    // AR: المفتاح الأجنبي الذي يشير إلى المشروع.
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    // EN: Foreign key referencing the assigned Association.
    // AR: المفتاح الأجنبي الذي يشير إلى الجمعية المعينة.
    public int AssociationId { get; set; }
    public Association Association { get; set; } = null!;

    // EN: Navigation collection of geographical locations associated with this project-association link.
    // AR: قائمة المواقع الجغرافية المرتبطة بتعيين هذه الجمعية في المشروع.
    public ICollection<ProjectAssociationLocation> Locations { get; set; } = new List<ProjectAssociationLocation>();
}