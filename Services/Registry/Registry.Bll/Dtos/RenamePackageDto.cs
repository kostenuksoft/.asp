using System.ComponentModel.DataAnnotations;

namespace Registry.Bll.Dtos;

public sealed class RenamePackageDto
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}
