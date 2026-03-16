namespace Tokvera;

public static class TokveraEvent
{
    public static Dictionary<string, object?> Create(
        TraceHandle handle,
        string status,
        FinishSpanOptions options,
        string outcome,
        string? retryReason,
        string? fallbackReason,
        string? qualityLabel,
        double? feedbackScore,
        Metrics metrics,
        Decision decision,
        Usage usage)
    {
        var tags = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["feature"] = handle.Options.Feature,
            ["tenant_id"] = handle.Options.TenantId,
            ["customer_id"] = handle.Options.CustomerId,
            ["attempt_type"] = handle.Options.AttemptType,
            ["plan"] = handle.Options.Plan,
            ["environment"] = handle.Options.Environment,
            ["template_id"] = handle.Options.TemplateId,
            ["trace_id"] = handle.TraceId,
            ["run_id"] = handle.RunId,
            ["conversation_id"] = handle.Options.ConversationId,
            ["span_id"] = handle.SpanId,
            ["parent_span_id"] = handle.ParentSpanId,
            ["step_name"] = handle.Options.StepName,
            ["outcome"] = outcome,
            ["retry_reason"] = retryReason,
            ["fallback_reason"] = fallbackReason,
            ["quality_label"] = qualityLabel,
            ["feedback_score"] = feedbackScore,
        };

        var eventPayload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["schema_version"] = TrackOptions.Choose(handle.Options.SchemaVersion, TokveraTracer.TraceSchemaVersionV2),
            ["event_type"] = handle.EventType,
            ["provider"] = handle.Provider,
            ["endpoint"] = handle.Endpoint,
            ["status"] = status,
            ["timestamp"] = DateTimeOffset.UtcNow,
            ["latency_ms"] = metrics.LatencyMs ?? 0,
            ["model"] = TrackOptions.Choose(handle.Model, "manual"),
            ["usage"] = usage.AsDictionary(),
            ["tags"] = PruneNulls(tags),
        };

        if (!string.IsNullOrWhiteSpace(outcome) || !string.IsNullOrWhiteSpace(retryReason) || !string.IsNullOrWhiteSpace(fallbackReason) || !string.IsNullOrWhiteSpace(qualityLabel) || feedbackScore is not null)
        {
            eventPayload["evaluation"] = PruneNulls(new Dictionary<string, object?>
            {
                ["outcome"] = outcome,
                ["retry_reason"] = retryReason,
                ["fallback_reason"] = fallbackReason,
                ["quality_label"] = qualityLabel,
                ["feedback_score"] = feedbackScore,
            });
        }

        if (!string.IsNullOrWhiteSpace(handle.Options.SpanKind))
        {
            eventPayload["span_kind"] = handle.Options.SpanKind;
        }

        if (!string.IsNullOrWhiteSpace(handle.Options.ToolName))
        {
            eventPayload["tool_name"] = handle.Options.ToolName;
        }

        if (handle.Options.PayloadRefs.Count > 0)
        {
            eventPayload["payload_refs"] = handle.Options.PayloadRefs.ToList();
        }

        var payloadBlocks = handle.Options.PayloadBlocks
            .Concat(options.PayloadBlocks)
            .Select(block => block.AsDictionary())
            .ToList();
        if (payloadBlocks.Count > 0)
        {
            eventPayload["payload_blocks"] = payloadBlocks;
        }

        if (!metrics.IsEmpty())
        {
            eventPayload["metrics"] = metrics.AsDictionary();
        }

        if (!decision.IsEmpty())
        {
            eventPayload["decision"] = decision.AsDictionary();
        }

        if (options.Error is not null)
        {
            eventPayload["error"] = options.Error.AsDictionary();
        }

        return eventPayload;
    }

    private static Dictionary<string, object?> PruneNulls(Dictionary<string, object?> values)
        => values.Where(entry => entry.Value is not null).ToDictionary(entry => entry.Key, entry => entry.Value);

    public sealed class Usage
    {
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public int? TotalTokens { get; set; }

        public Usage Copy() => new()
        {
            PromptTokens = PromptTokens,
            CompletionTokens = CompletionTokens,
            TotalTokens = TotalTokens,
        };

        public Dictionary<string, object> AsDictionary() => new(StringComparer.Ordinal)
        {
            ["prompt_tokens"] = PromptTokens ?? 0,
            ["completion_tokens"] = CompletionTokens ?? 0,
            ["total_tokens"] = TotalTokens ?? 0,
        };
    }

    public sealed class PayloadBlock(string payloadType, string content)
    {
        public string PayloadType { get; } = payloadType;
        public string Content { get; } = content;

        public PayloadBlock Copy() => new(PayloadType, Content);

        public Dictionary<string, object?> AsDictionary() => new(StringComparer.Ordinal)
        {
            ["payload_type"] = PayloadType,
            ["content"] = Content,
        };
    }

    public sealed class Metrics
    {
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public int? TotalTokens { get; set; }
        public double? CostUsd { get; set; }
        public long? LatencyMs { get; set; }

        public Metrics Copy() => new()
        {
            PromptTokens = PromptTokens,
            CompletionTokens = CompletionTokens,
            TotalTokens = TotalTokens,
            CostUsd = CostUsd,
            LatencyMs = LatencyMs,
        };

        public static Metrics Merge(Metrics? current, Metrics? overrideMetrics)
        {
            var merged = current?.Copy() ?? new Metrics();
            if (overrideMetrics is null)
            {
                return merged;
            }

            merged.PromptTokens = overrideMetrics.PromptTokens ?? merged.PromptTokens;
            merged.CompletionTokens = overrideMetrics.CompletionTokens ?? merged.CompletionTokens;
            merged.TotalTokens = overrideMetrics.TotalTokens ?? merged.TotalTokens;
            merged.CostUsd = overrideMetrics.CostUsd ?? merged.CostUsd;
            merged.LatencyMs = overrideMetrics.LatencyMs ?? merged.LatencyMs;
            return merged;
        }

        public bool IsEmpty()
            => PromptTokens is null && CompletionTokens is null && TotalTokens is null && CostUsd is null && LatencyMs is null;

        public Dictionary<string, object?> AsDictionary()
        {
            var values = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["prompt_tokens"] = PromptTokens,
                ["completion_tokens"] = CompletionTokens,
                ["total_tokens"] = TotalTokens,
                ["cost_usd"] = CostUsd,
                ["latency_ms"] = LatencyMs,
            };
            return PruneNulls(values);
        }
    }

    public sealed class Decision
    {
        public string? RetryReason { get; set; }
        public string? FallbackReason { get; set; }
        public string? RoutingReason { get; set; }
        public string? Route { get; set; }

        public Decision Copy() => new()
        {
            RetryReason = RetryReason,
            FallbackReason = FallbackReason,
            RoutingReason = RoutingReason,
            Route = Route,
        };

        public static Decision Merge(Decision? current, Decision? overrideDecision)
        {
            var merged = current?.Copy() ?? new Decision();
            if (overrideDecision is null)
            {
                return merged;
            }

            merged.RetryReason = TrackOptions.Choose(overrideDecision.RetryReason, merged.RetryReason);
            merged.FallbackReason = TrackOptions.Choose(overrideDecision.FallbackReason, merged.FallbackReason);
            merged.RoutingReason = TrackOptions.Choose(overrideDecision.RoutingReason, merged.RoutingReason);
            merged.Route = TrackOptions.Choose(overrideDecision.Route, merged.Route);
            return merged;
        }

        public bool IsEmpty()
            => string.IsNullOrWhiteSpace(RetryReason) &&
               string.IsNullOrWhiteSpace(FallbackReason) &&
               string.IsNullOrWhiteSpace(RoutingReason) &&
               string.IsNullOrWhiteSpace(Route);

        public Dictionary<string, object?> AsDictionary() => PruneNulls(new Dictionary<string, object?>
        {
            ["retry_reason"] = RetryReason,
            ["fallback_reason"] = FallbackReason,
            ["routing_reason"] = RoutingReason,
            ["route"] = Route,
        });
    }

    public sealed class EventError(string type, string message)
    {
        public string Type { get; } = type;
        public string Message { get; } = message;

        public Dictionary<string, object?> AsDictionary() => new(StringComparer.Ordinal)
        {
            ["type"] = Type,
            ["message"] = Message,
        };
    }
}
