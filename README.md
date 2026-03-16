# tokvera-dotnet

Preview .NET SDK for Tokvera tracing.

Current Wave 2 preview surface:
- manual tracer substrate
- lifecycle-capable root and child spans
- provider wrappers for OpenAI, Anthropic, Gemini, and Mistral
- OTel bridge
- runnable examples
- canonical contract check

This repo is not official until it clears:
- `dotnet test`
- canonical contract validation
- shared smoke/soak visibility in `tokvera`
- dashboard visibility in traces, live traces, and trace detail

## Install

The package is not published yet. Use the local project reference or source checkout.

## Quickstart

```csharp
using Tokvera;

var tracer = TokveraSdk.CreateTracer(new TrackOptions
{
    ApiKey = Environment.GetEnvironmentVariable("TOKVERA_API_KEY"),
    Feature = "existing_app",
    CaptureContent = true,
    EmitLifecycleEvents = true,
});

var trace = await tracer.StartTraceAsync();
var span = await tracer.StartSpanAsync(trace, new TrackOptions { StepName = "plan_response" });
await tracer.FinishSpanAsync(span);
```

## Examples

- `examples/ManualTracer`
- `examples/ProviderWrappers`
- `examples/OtelBridge`

## Contract check

```bash
node scripts/check-canonical-contract.mjs
```
