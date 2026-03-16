namespace Tokvera;

public sealed class ProviderRequest
{
    public string? Model { get; set; }
    public string? EventType { get; set; }
    public string? Endpoint { get; set; }
    public string? StepName { get; set; }
    public string? SpanKind { get; set; }
    public object? Input { get; set; }
    public string? ToolName { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
