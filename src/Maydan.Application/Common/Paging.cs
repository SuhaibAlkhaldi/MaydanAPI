namespace Maydan.Application.Common;

public static class Paging
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > MaxPageSize ? DefaultPageSize : pageSize;
        return (safePage, safePageSize);
    }
}
