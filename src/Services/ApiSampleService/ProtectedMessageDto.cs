namespace ApiSampleService;

internal sealed record ProtectedMessageDto(string Message, string? RequestedBy, DateTime Timestamp);
