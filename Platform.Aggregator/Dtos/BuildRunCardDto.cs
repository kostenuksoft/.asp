namespace Platform.Aggregator.Dtos;

public sealed record BuildRunCardDto(
    BuildRunSummaryDto Run,
    IReadOnlyList<PackageCardDto> Packages,
    IReadOnlyList<string> Warnings
);
