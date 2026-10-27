namespace Registry.Bll.Dtos;

public sealed record TagWithPackagesDto(
    long TagId,
    string Name,
    string? Description,
    IReadOnlyList<TaggedPackageDto> Packages
);
