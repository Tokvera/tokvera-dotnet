namespace Tokvera.Tests;

public sealed class TokveraOtelBridgeTests
{
    [Fact]
    public async Task ExportAsync_MapsProviderSpanToCanonicalEvent()
    {
        var handler = new RecordingHttpMessageHandler();
        var client = new TokveraClient("tk_test", "https://api.tokvera.org", new HttpClient(handler));
        var bridge = new TokveraOtelBridge(new TrackOptions
        {
            ApiKey = "tk_test",
            Feature = "runtime_otel_dotnet",
            Environment = "test",
        }, client);

        await bridge.ExportAsync(new[]
        {
            new OTelReadableSpan
            {
                Name = "openai_call",
                TraceId = "trc_otel_dotnet",
                SpanId = "spn_otel_dotnet",
                StartTime = DateTimeOffset.UtcNow.AddMilliseconds(-120),
                EndTime = DateTimeOffset.UtcNow,
                Attributes = new Dictionary<string, object?>
                {
                    ["llm.provider"] = "openai",
                    ["gen_ai.request.model"] = "gpt-5.1-mini",
                    ["gen_ai.usage.prompt_tokens"] = 7,
                    ["gen_ai.usage.completion_tokens"] = 5,
                    ["gen_ai.usage.total_tokens"] = 12,
                    ["tokvera.run_id"] = "run_otel_dotnet",
                },
            },
        });

        var payload = handler.Requests.Single();
        Assert.Equal("openai.request", payload["event_type"]!.GetValue<string>());
        Assert.Equal("responses.create", payload["endpoint"]!.GetValue<string>());
        Assert.Equal("runtime_otel_dotnet", payload["tags"]!["feature"]!.GetValue<string>());
        Assert.True(payload["metrics"]!["latency_ms"]!.GetValue<int>() >= 1);
    }
}
