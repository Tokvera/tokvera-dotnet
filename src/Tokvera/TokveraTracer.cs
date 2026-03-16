using System.Text.Json;

namespace Tokvera;

public sealed class TokveraTracer
{
    public const string TraceSchemaVersionV2 = "2026-04-01";

    private readonly TrackOptions _baseOptions;
    private readonly TokveraClient _client;

    public TokveraTracer(TrackOptions? baseOptions = null)
        : this(baseOptions, new TokveraClient(baseOptions?.ApiKey, baseOptions?.BaseUrl))
    {
    }

    public TokveraTracer(TrackOptions? baseOptions, TokveraClient client)
    {
        _baseOptions = baseOptions?.Copy() ?? TrackOptions.Create();
        _client = client;
    }

    public async Task<TraceHandle> StartTraceAsync(TrackOptions? options = null, CancellationToken cancellationToken = default)
    {
        var merged = TrackOptions.Merge(_baseOptions, options);
        var handle = new TraceHandle(
            TrackOptions.Choose(merged.TraceId, NewId("trc"))!,
            TrackOptions.Choose(merged.RunId, NewId("run"))!,
            TrackOptions.Choose(merged.SpanId, NewId("spn"))!,
            null,
            DateTimeOffset.UtcNow,
            TrackOptions.Choose(merged.Provider, "tokvera")!,
            TrackOptions.Choose(merged.EventType, "tokvera.trace")!,
            TrackOptions.Choose(merged.Endpoint, "manual.trace")!,
            TrackOptions.Choose(merged.Model, "manual")!,
            merged.Copy());
        Hydrate(handle.Options, handle);
        if (handle.Options.EmitLifecycleEvents)
        {
            await _client.IngestEventAsync(BuildEvent(handle, "in_progress", new FinishSpanOptions()), cancellationToken).ConfigureAwait(false);
        }

        return handle;
    }

    public async Task<TraceHandle> StartSpanAsync(TraceHandle parent, TrackOptions? options = null, CancellationToken cancellationToken = default)
    {
        var merged = TrackOptions.Merge(TrackOptions.Merge(_baseOptions, parent.Options), options);
        merged.TraceId = TrackOptions.Choose(options?.TraceId, parent.TraceId);
        merged.RunId = TrackOptions.Choose(options?.RunId, parent.RunId);
        merged.ParentSpanId = TrackOptions.Choose(options?.ParentSpanId, parent.SpanId);
        merged.SpanId = TrackOptions.Choose(options?.SpanId, NewId("spn"));
        merged.Provider = TrackOptions.Choose(options?.Provider, parent.Provider);
        merged.EventType = TrackOptions.Choose(options?.EventType, "tokvera.trace");
        merged.Endpoint = TrackOptions.Choose(options?.Endpoint, "manual.span");
        merged.Model = TrackOptions.Choose(options?.Model, parent.Model);

        var handle = new TraceHandle(
            merged.TraceId!,
            merged.RunId!,
            merged.SpanId!,
            merged.ParentSpanId,
            DateTimeOffset.UtcNow,
            merged.Provider!,
            merged.EventType!,
            merged.Endpoint!,
            TrackOptions.Choose(merged.Model, "manual")!,
            merged.Copy());
        Hydrate(handle.Options, handle);
        if (handle.Options.EmitLifecycleEvents)
        {
            await _client.IngestEventAsync(BuildEvent(handle, "in_progress", new FinishSpanOptions()), cancellationToken).ConfigureAwait(false);
        }

        return handle;
    }

    public TraceHandle AttachPayload(TraceHandle handle, object payload, string payloadType)
    {
        var updated = handle.Copy();
        var content = payload is string text ? text : JsonSerializer.Serialize(payload);
        updated.Options.PayloadBlocks.Add(new TokveraEvent.PayloadBlock(TrackOptions.Choose(payloadType, "other")!, content));
        return updated;
    }

    public Task FinishSpanAsync(TraceHandle handle, FinishSpanOptions? options = null, CancellationToken cancellationToken = default)
        => _client.IngestEventAsync(BuildEvent(handle, "success", options ?? new FinishSpanOptions()), cancellationToken);

    public Task FailSpanAsync(TraceHandle handle, Exception? error = null, FinishSpanOptions? options = null, CancellationToken cancellationToken = default)
    {
        var effective = options ?? new FinishSpanOptions();
        effective.Error ??= new TokveraEvent.EventError("runtime_error", error?.Message ?? "span failed");
        return _client.IngestEventAsync(BuildEvent(handle, "failure", effective), cancellationToken);
    }

    public Task<ProviderResult> TrackOpenAIAsync(TraceHandle parent, ProviderRequest request, Func<Task<ProviderResult>> operation, CancellationToken cancellationToken = default)
        => TrackProviderAsync(parent, "openai", request, operation, cancellationToken);

    public Task<ProviderResult> TrackAnthropicAsync(TraceHandle parent, ProviderRequest request, Func<Task<ProviderResult>> operation, CancellationToken cancellationToken = default)
        => TrackProviderAsync(parent, "anthropic", request, operation, cancellationToken);

    public Task<ProviderResult> TrackGeminiAsync(TraceHandle parent, ProviderRequest request, Func<Task<ProviderResult>> operation, CancellationToken cancellationToken = default)
        => TrackProviderAsync(parent, "gemini", request, operation, cancellationToken);

    public Task<ProviderResult> TrackMistralAsync(TraceHandle parent, ProviderRequest request, Func<Task<ProviderResult>> operation, CancellationToken cancellationToken = default)
        => TrackProviderAsync(parent, "mistral", request, operation, cancellationToken);

    public static string DefaultProviderEventType(string provider) => provider switch
    {
        "openai" => "openai.request",
        "anthropic" => "anthropic.request",
        "gemini" => "gemini.request",
        "mistral" => "mistral.request",
        _ => "tokvera.trace",
    };

    public static string DefaultProviderEndpoint(string provider) => provider switch
    {
        "openai" => "responses.create",
        "anthropic" => "messages.create",
        "gemini" => "models.generate_content",
        "mistral" => "chat.complete",
        _ => "manual.span",
    };

    private async Task<ProviderResult> TrackProviderAsync(
        TraceHandle parent,
        string provider,
        ProviderRequest request,
        Func<Task<ProviderResult>> operation,
        CancellationToken cancellationToken)
    {
        var child = await StartSpanAsync(parent, new TrackOptions
        {
            Provider = provider,
            EventType = TrackOptions.Choose(request.EventType, DefaultProviderEventType(provider)),
            Endpoint = TrackOptions.Choose(request.Endpoint, DefaultProviderEndpoint(provider)),
            Model = request.Model,
            StepName = TrackOptions.Choose(request.StepName, $"{provider}_call"),
            SpanKind = TrackOptions.Choose(request.SpanKind, "model"),
            ToolName = request.ToolName,
            Headers = new Dictionary<string, string>(request.Headers, StringComparer.OrdinalIgnoreCase),
        }, cancellationToken).ConfigureAwait(false);

        if (request.Input is not null && child.Options.CaptureContent)
        {
            child = AttachPayload(child, request.Input, "prompt_input");
        }

        ProviderResult result;
        try
        {
            result = await operation().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            await FailSpanAsync(child, error, new FinishSpanOptions(), cancellationToken).ConfigureAwait(false);
            throw;
        }

        if (result.Output is not null && child.Options.CaptureContent)
        {
            child = AttachPayload(child, result.Output, "model_output");
        }

        await FinishSpanAsync(child, new FinishSpanOptions
        {
            Usage = result.Usage,
            Outcome = TrackOptions.Choose(result.Outcome, "success"),
            QualityLabel = result.QualityLabel,
            FeedbackScore = result.FeedbackScore,
            Metrics = result.Metrics,
            Decision = result.Decision,
        }, cancellationToken).ConfigureAwait(false);

        return result;
    }

    private Dictionary<string, object?> BuildEvent(TraceHandle handle, string status, FinishSpanOptions options)
    {
        var metrics = TokveraEvent.Metrics.Merge(handle.Options.Metrics, options.Metrics);
        metrics.LatencyMs ??= Math.Max(1L, (long)(DateTimeOffset.UtcNow - handle.StartedAt).TotalMilliseconds);
        metrics.PromptTokens ??= options.Usage.PromptTokens;
        metrics.CompletionTokens ??= options.Usage.CompletionTokens;
        metrics.TotalTokens ??= options.Usage.TotalTokens;

        var usage = options.Usage.Copy();
        usage.PromptTokens ??= metrics.PromptTokens;
        usage.CompletionTokens ??= metrics.CompletionTokens;
        usage.TotalTokens ??= metrics.TotalTokens;

        var decision = TokveraEvent.Decision.Merge(handle.Options.Decision, options.Decision);
        var outcome = TrackOptions.Choose(options.Outcome, handle.Options.Outcome, status == "failure" ? "failure" : "success")!;
        var retryReason = TrackOptions.Choose(handle.Options.RetryReason, decision.RetryReason);
        var fallbackReason = TrackOptions.Choose(handle.Options.FallbackReason, decision.FallbackReason);
        var qualityLabel = TrackOptions.Choose(options.QualityLabel, handle.Options.QualityLabel);
        var feedbackScore = options.FeedbackScore ?? handle.Options.FeedbackScore;

        return TokveraEvent.Create(handle, status, options, outcome, retryReason, fallbackReason, qualityLabel, feedbackScore, metrics, decision, usage);
    }

    private static void Hydrate(TrackOptions options, TraceHandle handle)
    {
        options.TraceId = handle.TraceId;
        options.RunId = handle.RunId;
        options.SpanId = handle.SpanId;
        options.ParentSpanId = handle.ParentSpanId;
        options.Provider = handle.Provider;
        options.EventType = handle.EventType;
        options.Endpoint = handle.Endpoint;
        options.Model = handle.Model;
        options.StepName = TrackOptions.Choose(options.StepName, handle.ParentSpanId is null ? "trace_root" : "span_step");
        options.SpanKind = TrackOptions.Choose(options.SpanKind, "orchestrator");
        options.SchemaVersion = TrackOptions.Choose(options.SchemaVersion, TraceSchemaVersionV2);
    }

    private static string NewId(string prefix) => $"{prefix}_{Guid.NewGuid():N}";
}
