using Tokvera;
using Tokvera.Examples.Internal;

var tracer = TokveraSdk.CreateTracer(new TrackOptions
{
    ApiKey = ExampleEnv.ApiKey(),
    BaseUrl = ExampleEnv.BaseUrl(),
    Feature = ExampleEnv.Feature("runtime_provider_wrappers_dotnet"),
    TenantId = ExampleEnv.TenantId(),
    Environment = ExampleEnv.RuntimeEnvironment(),
    CaptureContent = true,
    EmitLifecycleEvents = true,
});

var root = await tracer.StartTraceAsync(new TrackOptions
{
    StepName = "trace_root",
});

await tracer.TrackOpenAIAsync(root, new ProviderRequest
{
    Model = "gpt-5.1-mini",
    Input = new { prompt = "Say hello from Tokvera .NET" },
}, () => Task.FromResult(new ProviderResult
{
    Model = "gpt-5.1-mini",
    Output = new { text = "hello" },
    Usage = new TokveraEvent.Usage
    {
        PromptTokens = 12,
        CompletionTokens = 7,
        TotalTokens = 19,
    },
    Metrics = new TokveraEvent.Metrics
    {
        CostUsd = 0.00031,
    },
}));

await tracer.FinishSpanAsync(root);
