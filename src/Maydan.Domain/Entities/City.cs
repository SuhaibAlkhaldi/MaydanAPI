using Maydan.Domain.Common;

namespace Maydan.Domain.Entities;

public class City : SharedEntities
{
    public string ArabicName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;

    public int CountryId { get; set; }
    public Country Country { get; set; } = null!;

    public ICollection<Association> Associations { get; set; } = new List<Association>();
    public ICollection<Association> AssociationsV2 { get; set; } = new List<Association>();
}
