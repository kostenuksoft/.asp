namespace Cicd.Bll.Dtos;

public sealed record RunStatusChangeDto(
    string? FromStatus,
    string ToStatus,
    string? Reason,
    DateTime CreatedAt,
    string CreatedBy
);
