namespace Platform.EventBus;

public sealed record EventSubscription(
    string Exchange,
    string Queue,
    IReadOnlyList<string> RoutingPatterns
)
{
    public string RetryQueue => $"{Queue}.retry";

    public string DeadLetterQueue => $"{Queue}.dlq";
}
