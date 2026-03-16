namespace Tokvera.Tests;

public sealed class TokveraClientTests
{
    [Theory]
    [InlineData("https://api.tokvera.org/v1/events", "https://api.tokvera.org")]
    [InlineData("\"https://api.tokvera.org/\"", "https://api.tokvera.org")]
    [InlineData(null, "https://api.tokvera.org")]
    public void NormalizeBaseUrl_StripsQuotesAndEventSuffix(string? input, string expected)
    {
        Assert.Equal(expected, TokveraClient.NormalizeBaseUrl(input));
    }

    [Fact]
    public async Task IngestEventAsync_SerializesTimestampAsIsoString()
    {
        var handler = new RecordingHttpMessageHandler();
        var client = new TokveraClient("tk_test", "https://api.tokvera.org", new HttpClient(handler));
        await client.IngestEventAsync(new Dictionary<string, object?>
        {
            ["timestamp"] = new DateTimeOffset(2026, 3, 16, 10, 11, 12, TimeSpan.Zero),
        });

        var timestamp = handler.Requests.Single()["timestamp"]!.GetValue<string>();
        Assert.StartsWith("2026-03-16T10:11:12", timestamp);
    }
}
