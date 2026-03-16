namespace Tokvera;

public sealed class OTelReadableSpan
{
    public string? Name { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? ParentSpanId { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public string? StatusCode { get; set; }
    public string? StatusDescription { get; set; }
    public Dictionary<string, object?> Attributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, object?> ResourceAttributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
