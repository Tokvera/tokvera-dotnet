namespace Tokvera;

public sealed class FinishSpanOptions
{
    public TokveraEvent.Usage Usage { get; set; } = new();
    public string? Outcome { get; set; }
    public string? QualityLabel { get; set; }
    public double? FeedbackScore { get; set; }
    public List<TokveraEvent.PayloadBlock> PayloadBlocks { get; } = new();
    public TokveraEvent.Metrics? Metrics { get; set; }
    public TokveraEvent.Decision? Decision { get; set; }
    public TokveraEvent.EventError? Error { get; set; }
}
