using System.ComponentModel.DataAnnotations;

namespace Registry.Bll.Dtos;

public sealed class CreatePackageDto
{
    [Range(1, long.MaxValue)]
    public long PublisherId { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(400)]
    public string? ProjectUrl { get; set; }

    public decimal LicensePrice { get; set; }
}
