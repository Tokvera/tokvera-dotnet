using Tokvera;
using Tokvera.Examples.Internal;

var tracer = TokveraSdk.CreateTracer(new TrackOptions
{
    ApiKey = ExampleEnv.ApiKey(),
    BaseUrl = ExampleEnv.BaseUrl(),
    Feature = ExampleEnv.Feature("runtime_existing_app_dotnet"),
    TenantId = ExampleEnv.TenantId(),
    Environment = ExampleEnv.RuntimeEnvironment(),
    CaptureContent = true,
    EmitLifecycleEvents = true,
});

var root = await tracer.StartTraceAsync(new TrackOptions
{
    StepName = "trace_root",
    Model = "manual",
});

var span = await tracer.StartSpanAsync(root, new TrackOptions
{
    StepName = "manual_plan",
});
span = tracer.AttachPayload(span, new { input = "Hello from .NET" }, "prompt_input");
await tracer.FinishSpanAsync(span, new FinishSpanOptions
{
    Usage = new TokveraEvent.Usage
    {
        PromptTokens = 8,
        CompletionTokens = 6,
        TotalTokens = 14,
    },
    Metrics = new TokveraEvent.Metrics
    {
        CostUsd = 0.00018,
    },
});

await tracer.FinishSpanAsync(root, new FinishSpanOptions
{
    Outcome = "success",
});
