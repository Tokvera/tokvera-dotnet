namespace Tokvera;

public sealed record TraceHandle(
    string TraceId,
    string RunId,
    string SpanId,
    string? ParentSpanId,
    DateTimeOffset StartedAt,
    string Provider,
    string EventType,
    string Endpoint,
    string Model,
    TrackOptions Options)
{
    public TraceHandle Copy() => new(
        TraceId,
        RunId,
        SpanId,
        ParentSpanId,
        StartedAt,
        Provider,
        EventType,
        Endpoint,
        Model,
        Options.Copy());
}
