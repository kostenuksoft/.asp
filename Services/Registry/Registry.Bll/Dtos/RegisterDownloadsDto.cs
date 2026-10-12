using System.ComponentModel.DataAnnotations;

namespace Registry.Bll.Dtos;

public sealed class RegisterDownloadsDto
{
    [Range(1, 1_000_000)]
    public long Count { get; set; }
}
