using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Services;

public sealed class ListQuery
{
    public const int Size = 10;

    public int? Page { get; set; }
    public string? Sort { get; set; }
    public string? Dir { get; set; }
    public string? Q { get; set; }
    public string? Filter { get; set; }

    public bool Paged => Page is > 0;
    public int NormalizedPage => Page is > 0 ? Page.Value : 1;
    public bool Desc => string.Equals(Dir, "desc", StringComparison.OrdinalIgnoreCase);
    public string? Term => string.IsNullOrWhiteSpace(Q) ? null : Q.Trim().ToLowerInvariant();
}

public static class Pages
{
    public static async Task<PageDto<T>> Materialize<T>(IQueryable<T> query, ListQuery list, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)ListQuery.Size));
        var page = total == 0 ? 1 : Math.Min(list.NormalizedPage, pages);
        var items = total == 0
            ? (IReadOnlyList<T>)[]
            : await query.Skip((page - 1) * ListQuery.Size).Take(ListQuery.Size).ToListAsync(ct);
        return new PageDto<T>(items, total, page, ListQuery.Size);
    }
}
