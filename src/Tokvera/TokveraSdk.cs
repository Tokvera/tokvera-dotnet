using System.Text.Json;

namespace Tokvera;

public static class TokveraSdk
{
    public static TokveraTracer CreateTracer(TrackOptions? options = null, TokveraClient? client = null)
        => client is null ? new TokveraTracer(options) : new TokveraTracer(options, client);

    public static TokveraOtelBridge CreateOtelBridge(TrackOptions? options = null, TokveraClient? client = null)
        => client is null ? new TokveraOtelBridge(options) : new TokveraOtelBridge(options, client);

    public static TrackOptions GetTrackOptionsFromTraceContext(TraceHandle handle, TrackOptions? options = null)
    {
        var merged = TrackOptions.Merge(handle.Options, options);
        merged.TraceId = handle.TraceId;
        merged.RunId = handle.RunId;
        merged.SpanId = handle.SpanId;
        merged.ParentSpanId = handle.ParentSpanId;
        merged.Provider = handle.Provider;
        merged.EventType = handle.EventType;
        merged.Endpoint = handle.Endpoint;
        merged.Model = handle.Model;
        return merged;
    }

    public static TokveraEvent.PayloadBlock CreatePayloadBlock(string payloadType, object payload)
        => new(payloadType, payload is string text ? text : JsonSerializer.Serialize(payload));
}
