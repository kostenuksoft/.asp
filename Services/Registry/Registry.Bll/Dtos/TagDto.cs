namespace Registry.Bll.Dtos;

public sealed record TagDto(
    long TagId,
    string Name,
    string? Description
);
