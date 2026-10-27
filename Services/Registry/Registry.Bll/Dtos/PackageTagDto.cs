namespace Registry.Bll.Dtos;

public sealed record PackageTagDto(
    long TagId,
    string TagName,
    DateTime AssignedAt
);
