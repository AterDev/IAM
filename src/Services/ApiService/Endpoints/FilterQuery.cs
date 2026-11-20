namespace ApiService.Endpoints;

/// <summary>Applies the common paging query fields omitted by source-generated metadata for inherited filter properties.</summary>
internal static class FilterQuery
{
    public static T Apply<T>(T filter, int? pageIndex, int? pageSize, string? orderBy)
        where T : FilterBase
    {
        if (pageIndex is > 0)
        {
            filter.PageIndex = pageIndex.Value;
        }

        if (pageSize is >= 0)
        {
            filter.PageSize = pageSize.Value;
        }

        filter.OrderBy = ParseOrderBy(orderBy);
        return filter;
    }

    private static Dictionary<string, bool>? ParseOrderBy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = item.IndexOfAny(['=', ':']);
            if (separator <= 0)
            {
                continue;
            }

            var field = item[..separator].Trim();
            if (field.Length > 0 && bool.TryParse(item[(separator + 1)..].Trim(), out var ascending))
            {
                result[field] = ascending;
            }
        }

        return result.Count == 0 ? null : result;
    }
}
