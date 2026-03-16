namespace Tokvera;

public sealed class TokveraOtelBridge
{
    private readonly TrackOptions _baseOptions;
    private readonly TokveraTracer _tracer;

    public TokveraOtelBridge(TrackOptions? baseOptions = null)
        : this(baseOptions, new TokveraClient(baseOptions?.ApiKey, baseOptions?.BaseUrl))
    {
    }

    public TokveraOtelBridge(TrackOptions? baseOptions, TokveraClient client)
    {
        _baseOptions = baseOptions?.Copy() ?? TrackOptions.Create();
        _tracer = new TokveraTracer(_baseOptions, client);
    }

    public async Task ExportAsync(IEnumerable<OTelReadableSpan> spans, CancellationToken cancellationToken = default)
    {
        foreach (var span in spans)
        {
            var provider = Value(Attribute(span.Attributes, "tokvera.provider"), Attribute(span.Attributes, "llm.provider"), "tokvera")!;
            var options = new TrackOptions
            {
                TraceId = Value(span.TraceId, $"trc_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"),
                RunId = Value(Attribute(span.Attributes, "tokvera.run_id"), span.TraceId, $"run_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"),
                SpanId = Value(span.SpanId, $"spn_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"),
                ParentSpanId = span.ParentSpanId,
                Feature = Value(Attribute(span.Attributes, "tokvera.feature"), _baseOptions.Feature),
                TenantId = Value(Attribute(span.Attributes, "tokvera.tenant_id"), _baseOptions.TenantId),
                CustomerId = Value(Attribute(span.Attributes, "tokvera.customer_id"), _baseOptions.CustomerId),
                Environment = Value(Attribute(span.ResourceAttributes, "deployment.environment"), _baseOptions.Environment),
                ConversationId = Attribute(span.Attributes, "tokvera.conversation_id"),
                StepName = Value(Attribute(span.Attributes, "tokvera.step_name"), span.Name),
                SpanKind = Value(Attribute(span.Attributes, "tokvera.span_kind"), "orchestrator"),
                Provider = provider,
                EventType = Value(Attribute(span.Attributes, "tokvera.event_type"), provider == "tokvera" ? "tokvera.trace" : TokveraTracer.DefaultProviderEventType(provider)),
                Endpoint = Value(Attribute(span.Attributes, "tokvera.endpoint"), provider == "tokvera" ? "otel.span" : TokveraTracer.DefaultProviderEndpoint(provider)),
                Model = Value(Attribute(span.Attributes, "tokvera.model"), Attribute(span.Attributes, "gen_ai.request.model")),
                SchemaVersion = TokveraTracer.TraceSchemaVersionV2,
            };

            var handle = new TraceHandle(
                options.TraceId!,
                options.RunId!,
                options.SpanId!,
                options.ParentSpanId,
                span.StartTime ?? DateTimeOffset.UtcNow,
                options.Provider!,
                options.EventType!,
                options.Endpoint!,
                TrackOptions.Choose(options.Model, "manual")!,
                options);

            var metrics = new TokveraEvent.Metrics
            {
                PromptTokens = AttributeInt(span.Attributes, "gen_ai.usage.prompt_tokens"),
                CompletionTokens = AttributeInt(span.Attributes, "gen_ai.usage.completion_tokens"),
                TotalTokens = AttributeInt(span.Attributes, "gen_ai.usage.total_tokens"),
                LatencyMs = span.EndTime is not null && span.StartTime is not null
                    ? Math.Max(1L, (long)(span.EndTime.Value - span.StartTime.Value).TotalMilliseconds)
                    : 1L,
            };

            if (string.Equals(span.StatusCode, "error", StringComparison.OrdinalIgnoreCase))
            {
                await _tracer.FailSpanAsync(handle, new InvalidOperationException(Value(span.StatusDescription, "otel span failed")!), new FinishSpanOptions
                {
                    Metrics = metrics,
                    Error = new TokveraEvent.EventError("otel_error", Value(span.StatusDescription, "otel span failed")!),
                }, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _tracer.FinishSpanAsync(handle, new FinishSpanOptions
                {
                    Metrics = metrics,
                }, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static string? Attribute(Dictionary<string, object?> values, string key)
        => values.TryGetValue(key, out var value) ? value?.ToString() : null;

    private static int? AttributeInt(Dictionary<string, object?> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            double doubleValue => (int)doubleValue,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => null,
        };
    }

    private static string? Value(params string?[] values)
        => TrackOptions.Choose(values);
}
