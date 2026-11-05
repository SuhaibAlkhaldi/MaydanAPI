using Maydan.Domain.Common;

namespace Maydan.Domain.Entities;

public class LookupCategory : SharedEntities
{
    public string Code { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;

    public ICollection<LookupItem> Items { get; set; } = new List<LookupItem>();
}
