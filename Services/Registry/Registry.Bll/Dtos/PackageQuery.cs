using System.ComponentModel.DataAnnotations;

namespace Registry.Bll.Dtos;

public sealed class PackageQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, int.MaxValue)]
    public int PageSize { get; set; } = DefaultPageSize;

    [StringLength(200)]
    public string? Search { get; set; }

    public long? PublisherId { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public string SortBy { get; set; } = "name";

    public string SortDir { get; set; } = "asc";
}
