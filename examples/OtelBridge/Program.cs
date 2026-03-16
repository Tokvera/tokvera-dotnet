using Tokvera;
using Tokvera.Examples.Internal;

var bridge = TokveraSdk.CreateOtelBridge(new TrackOptions
{
    ApiKey = ExampleEnv.ApiKey(),
    BaseUrl = ExampleEnv.BaseUrl(),
    Feature = ExampleEnv.Feature("runtime_otel_dotnet"),
    TenantId = ExampleEnv.TenantId(),
    Environment = ExampleEnv.RuntimeEnvironment(),
});

var now = DateTimeOffset.UtcNow;
await bridge.ExportAsync(new[]
{
    new OTelReadableSpan
    {
        Name = "openai_call",
        TraceId = $"trc_{Guid.NewGuid():N}",
        SpanId = $"spn_{Guid.NewGuid():N}",
        StartTime = now.AddMilliseconds(-90),
        EndTime = now,
        Attributes = new Dictionary<string, object?>
        {
            ["llm.provider"] = "openai",
            ["gen_ai.request.model"] = "gpt-5.1-mini",
            ["gen_ai.usage.prompt_tokens"] = 9,
            ["gen_ai.usage.completion_tokens"] = 5,
            ["gen_ai.usage.total_tokens"] = 14,
            ["tokvera.run_id"] = $"run_{Guid.NewGuid():N}",
        },
    },
});
