namespace Tokvera.Tests;

public sealed class TokveraTracerTests
{
    [Fact]
    public async Task StartTraceAndStartSpan_EmitLifecycleAndInheritIds()
    {
        var handler = new RecordingHttpMessageHandler();
        var client = new TokveraClient("tk_test", "https://api.tokvera.org", new HttpClient(handler));
        var tracer = new TokveraTracer(new TrackOptions
        {
            ApiKey = "tk_test",
            Feature = "runtime_existing_app_dotnet",
            EmitLifecycleEvents = true,
        }, client);

        var root = await tracer.StartTraceAsync();
        var child = await tracer.StartSpanAsync(root, new TrackOptions
        {
            StepName = "child_step",
        });

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("in_progress", handler.Requests[0]["status"]!.GetValue<string>());
        Assert.Equal(root.TraceId, handler.Requests[0]["tags"]!["trace_id"]!.GetValue<string>());
        Assert.Equal(root.TraceId, child.TraceId);
        Assert.Equal(root.RunId, child.RunId);
        Assert.Equal(root.SpanId, child.ParentSpanId);
        Assert.Equal("child_step", child.Options.StepName);
    }

    [Fact]
    public async Task TrackOpenAiAsync_CapturesPayloadsAndUsage()
    {
        var handler = new RecordingHttpMessageHandler();
        var client = new TokveraClient("tk_test", "https://api.tokvera.org", new HttpClient(handler));
        var tracer = new TokveraTracer(new TrackOptions
        {
            ApiKey = "tk_test",
            CaptureContent = true,
            Feature = "runtime_provider_wrappers_dotnet",
        }, client);

        var root = await tracer.StartTraceAsync();
        await tracer.TrackOpenAIAsync(root, new ProviderRequest
        {
            Model = "gpt-5-mini",
            Input = new { prompt = "Say hello" },
        }, () => Task.FromResult(new ProviderResult
        {
            Model = "gpt-5-mini",
            Output = new { answer = "hello" },
            Usage = new TokveraEvent.Usage
            {
                PromptTokens = 10,
                CompletionTokens = 4,
                TotalTokens = 14,
            },
            Metrics = new TokveraEvent.Metrics
            {
                CostUsd = 0.00042,
            },
        }));

        var payload = handler.Requests.Last();
        Assert.Equal("openai.request", payload["event_type"]!.GetValue<string>());
        Assert.Equal("responses.create", payload["endpoint"]!.GetValue<string>());
        Assert.Equal(14, payload["usage"]!["total_tokens"]!.GetValue<int>());
        Assert.Equal(2, payload["payload_blocks"]!.AsArray().Count);
    }
}
