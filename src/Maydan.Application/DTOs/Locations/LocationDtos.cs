namespace Maydan.Application.DTOs.Locations;

// Field names below are deliberately NOT the usual EnglishName/ArabicName pair this codebase uses
// elsewhere (Project, ProjectType, Group, ...) — they match workforcment's own
// already-written frontend contract this DTO has to satisfy, not a convention this pass gets to
// pick.
public class CityDto
{
    public int Id { get; set; }
    public string CityEnglishName { get; set; } = string.Empty;
    public string CityArabicName { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}

public class CreateCityDto
{
    public string CityEnglishName { get; set; } = string.Empty;
    public string CityArabicName { get; set; } = string.Empty;
}

public class UpdateCityDto
{
    public string CityEnglishName { get; set; } = string.Empty;
    public string CityArabicName { get; set; } = string.Empty;
}

// EnglishName/ArabicName/CityId pair, NOT a CityLocation-prefixed variant — this is what
// workforcment's own city-locations.service.ts (CityLocation/CreateCityLocation/UpdateCityLocation
// interfaces) actually expects, confirmed by reading that file before writing this one. Don't
// module area; that convention exists there because location.models.ts specifically demands it,
// and city-locations.service.ts demands something different.
public class CityLocationDto
{
    public int Id { get; set; }
    public string EnglishName { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public int CityId { get; set; }
    public bool IsDeleted { get; set; }
}

public class CreateCityLocationDto
{
    public string EnglishName { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public int CityId { get; set; }
}

// Id lives in the body here (not the route) — same convention UpdateAssociationDto/
// ProjectsController.Update already use, and what city-locations.service.ts's own update() actually
// sends (PUT /api/CityLocations with { id, englishName, arabicName, cityId }, not PUT
// /api/CityLocations/{id}).
public class UpdateCityLocationDto : CreateCityLocationDto
{
    public int Id { get; set; }
}
