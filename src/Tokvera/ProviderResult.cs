namespace Tokvera;

public sealed class ProviderResult
{
    public string? Model { get; set; }
    public object? Output { get; set; }
    public TokveraEvent.Usage Usage { get; set; } = new();
    public string? Outcome { get; set; }
    public string? QualityLabel { get; set; }
    public double? FeedbackScore { get; set; }
    public TokveraEvent.Metrics? Metrics { get; set; }
    public TokveraEvent.Decision? Decision { get; set; }
}
