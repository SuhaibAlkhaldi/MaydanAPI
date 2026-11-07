using Maydan.Domain.Common;

namespace Maydan.Domain.Entities;

/// <summary>
/// EN: Represents a specific physical/geographical location for a ProjectAssociation assignment.
/// AR: يمثل موقعاً جغرافياً تفصيلياً تابعاً لجمعية معينة داخل مشروع محدد.
/// </summary>
public class ProjectAssociationLocation : SharedEntities
{
    // EN: Foreign key referencing the parent ProjectAssociation record.
    // AR: المفتاح الأجنبي الذي يشير إلى سجل ربط المشروع بالجمعية الأب.
    public int ProjectAssociationId { get; set; }
    public ProjectAssociation ProjectAssociation { get; set; } = null!;

    // EN: Name or title of the location.
    // AR: اسم أو عنوان الموقع.
    public string Name { get; set; } = string.Empty;

    // EN: Optional description or notes about the location.
    // AR: وصف اختياري أو ملاحظات إضافية حول الموقع.
    public string? Description { get; set; }

    // EN: Direct Google Maps URL for the location.
    // AR: رابط الموقع المباشر على خرائط جوجل.
    public string? LocationOnGoogleMaps { get; set; }

    // EN: Latitude coordinate stored as string to match frontend payload directly.
    // AR: إحداثيات خط العرض مخزنة كنص للتوافق المباشر مع بيانات الفرونت إند.
    public string? Latitude { get; set; }

    // EN: Longitude coordinate stored as string to match frontend payload directly.
    // AR: إحداثيات خط الطول مخزنة كنص للتوافق المباشر مع بيانات الفرونت إند.
    public string? Longitude { get; set; }
}