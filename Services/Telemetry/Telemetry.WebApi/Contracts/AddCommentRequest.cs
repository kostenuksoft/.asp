namespace Telemetry.WebApi.Contracts;

public sealed record AddCommentRequest(
    string Author,
    string Body,
    int Version
);
