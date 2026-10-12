using System.ComponentModel.DataAnnotations;

namespace Registry.Bll.Dtos;

public sealed class AssignTagDto
{
    [Range(1, long.MaxValue)]
    public long TagId { get; set; }
}
