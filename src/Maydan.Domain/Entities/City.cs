using Maydan.Domain.Common;

namespace Maydan.Domain.Entities;

public class City : SharedEntities
{
    public string ArabicName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;

    public ICollection<Association> Associations { get; set; } = new List<Association>();
}
