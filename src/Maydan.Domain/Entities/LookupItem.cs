using Maydan.Domain.Common;

namespace Maydan.Domain.Entities;

public class LookupItem : SharedEntities
{
    public int CategoryId { get; set; }
    public LookupCategory Category { get; set; } = null!;

    public string Code { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
