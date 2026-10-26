namespace Platform.EventBus;

public sealed class UnprocessableMessageException(string message) : Exception(message);
