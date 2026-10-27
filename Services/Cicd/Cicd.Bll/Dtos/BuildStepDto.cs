namespace Cicd.Bll.Dtos;

public sealed record BuildStepDto(
    long BuildStepId,
    int StepOrder,
    string Name,
    string Status,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    int? ExitCode
);
