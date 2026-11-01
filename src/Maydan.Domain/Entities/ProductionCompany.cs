using Maydan.Domain.Common;

namespace Maydan.Domain.Entities;

public class ProductionCompany : SharedEntities
{
    public string ArabicName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public bool IsSelfRegistered { get; set; }

    // Entity onboarding Stage 1 (2026-09-22): the public self-registration form collects both, so
    // they're persisted rather than dropped. RegistrationNumber is enforced unique (confirmed
    // product decision) — see ProductionCompanyConfiguration. CityId mirrors Association.CityId/City.
    public string RegistrationNumber { get; set; } = string.Empty;
    public int CityId { get; set; }
    public City City { get; set; } = null!;
    public int? CreatedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
