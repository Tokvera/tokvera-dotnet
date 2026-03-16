namespace Tokvera;

public sealed class TrackOptions
{
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public string? Feature { get; set; }
    public string? TenantId { get; set; }
    public string? CustomerId { get; set; }
    public string? AttemptType { get; set; }
    public string? Plan { get; set; }
    public string? Environment { get; set; }
    public string? TemplateId { get; set; }
    public string? TraceId { get; set; }
    public string? RunId { get; set; }
    public string? ConversationId { get; set; }
    public string? SpanId { get; set; }
    public string? ParentSpanId { get; set; }
    public string? StepName { get; set; }
    public string? Outcome { get; set; }
    public string? RetryReason { get; set; }
    public string? FallbackReason { get; set; }
    public string? QualityLabel { get; set; }
    public double? FeedbackScore { get; set; }
    public bool CaptureContent { get; set; }
    public bool EmitLifecycleEvents { get; set; }
    public string? SchemaVersion { get; set; }
    public string? SpanKind { get; set; }
    public string? ToolName { get; set; }
    public string? Provider { get; set; }
    public string? EventType { get; set; }
    public string? Endpoint { get; set; }
    public string? Model { get; set; }
    public List<TokveraEvent.PayloadBlock> PayloadBlocks { get; } = new();
    public List<string> PayloadRefs { get; } = new();
    public TokveraEvent.Metrics? Metrics { get; set; }
    public TokveraEvent.Decision? Decision { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static TrackOptions Create() => new();

    public TrackOptions Copy()
    {
        var copy = new TrackOptions
        {
            ApiKey = ApiKey,
            BaseUrl = BaseUrl,
            Feature = Feature,
            TenantId = TenantId,
            CustomerId = CustomerId,
            AttemptType = AttemptType,
            Plan = Plan,
            Environment = Environment,
            TemplateId = TemplateId,
            TraceId = TraceId,
            RunId = RunId,
            ConversationId = ConversationId,
            SpanId = SpanId,
            ParentSpanId = ParentSpanId,
            StepName = StepName,
            Outcome = Outcome,
            RetryReason = RetryReason,
            FallbackReason = FallbackReason,
            QualityLabel = QualityLabel,
            FeedbackScore = FeedbackScore,
            CaptureContent = CaptureContent,
            EmitLifecycleEvents = EmitLifecycleEvents,
            SchemaVersion = SchemaVersion,
            SpanKind = SpanKind,
            ToolName = ToolName,
            Provider = Provider,
            EventType = EventType,
            Endpoint = Endpoint,
            Model = Model,
            Metrics = Metrics?.Copy(),
            Decision = Decision?.Copy(),
            Headers = new Dictionary<string, string>(Headers, StringComparer.OrdinalIgnoreCase),
        };
        copy.PayloadBlocks.AddRange(PayloadBlocks.Select(block => block.Copy()));
        copy.PayloadRefs.AddRange(PayloadRefs);
        return copy;
    }

    public static TrackOptions Merge(TrackOptions? current, TrackOptions? overrideOptions)
    {
        var merged = current?.Copy() ?? Create();
        if (overrideOptions is null)
        {
            return merged;
        }

        merged.ApiKey = Choose(overrideOptions.ApiKey, merged.ApiKey);
        merged.BaseUrl = Choose(overrideOptions.BaseUrl, merged.BaseUrl);
        merged.Feature = Choose(overrideOptions.Feature, merged.Feature);
        merged.TenantId = Choose(overrideOptions.TenantId, merged.TenantId);
        merged.CustomerId = Choose(overrideOptions.CustomerId, merged.CustomerId);
        merged.AttemptType = Choose(overrideOptions.AttemptType, merged.AttemptType);
        merged.Plan = Choose(overrideOptions.Plan, merged.Plan);
        merged.Environment = Choose(overrideOptions.Environment, merged.Environment);
        merged.TemplateId = Choose(overrideOptions.TemplateId, merged.TemplateId);
        merged.TraceId = Choose(overrideOptions.TraceId, merged.TraceId);
        merged.RunId = Choose(overrideOptions.RunId, merged.RunId);
        merged.ConversationId = Choose(overrideOptions.ConversationId, merged.ConversationId);
        merged.SpanId = Choose(overrideOptions.SpanId, merged.SpanId);
        merged.ParentSpanId = Choose(overrideOptions.ParentSpanId, merged.ParentSpanId);
        merged.StepName = Choose(overrideOptions.StepName, merged.StepName);
        merged.Outcome = Choose(overrideOptions.Outcome, merged.Outcome);
        merged.RetryReason = Choose(overrideOptions.RetryReason, merged.RetryReason);
        merged.FallbackReason = Choose(overrideOptions.FallbackReason, merged.FallbackReason);
        merged.QualityLabel = Choose(overrideOptions.QualityLabel, merged.QualityLabel);
        merged.FeedbackScore = overrideOptions.FeedbackScore ?? merged.FeedbackScore;
        merged.CaptureContent = merged.CaptureContent || overrideOptions.CaptureContent;
        merged.EmitLifecycleEvents = merged.EmitLifecycleEvents || overrideOptions.EmitLifecycleEvents;
        merged.SchemaVersion = Choose(overrideOptions.SchemaVersion, merged.SchemaVersion);
        merged.SpanKind = Choose(overrideOptions.SpanKind, merged.SpanKind);
        merged.ToolName = Choose(overrideOptions.ToolName, merged.ToolName);
        merged.Provider = Choose(overrideOptions.Provider, merged.Provider);
        merged.EventType = Choose(overrideOptions.EventType, merged.EventType);
        merged.Endpoint = Choose(overrideOptions.Endpoint, merged.Endpoint);
        merged.Model = Choose(overrideOptions.Model, merged.Model);

        if (overrideOptions.PayloadBlocks.Count > 0)
        {
            merged.PayloadBlocks.Clear();
            merged.PayloadBlocks.AddRange(overrideOptions.PayloadBlocks.Select(block => block.Copy()));
        }

        if (overrideOptions.PayloadRefs.Count > 0)
        {
            merged.PayloadRefs.Clear();
            merged.PayloadRefs.AddRange(overrideOptions.PayloadRefs);
        }

        merged.Metrics = overrideOptions.Metrics?.Copy() ?? merged.Metrics;
        merged.Decision = overrideOptions.Decision?.Copy() ?? merged.Decision;

        if (overrideOptions.Headers.Count > 0)
        {
            merged.Headers = new Dictionary<string, string>(overrideOptions.Headers, StringComparer.OrdinalIgnoreCase);
        }

        return merged;
    }

    internal static string? Choose(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
