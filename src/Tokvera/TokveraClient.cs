using System.Text.Json;

namespace Tokvera;

public sealed class TokveraClient
{
    public const string DefaultBaseUrl = "https://api.tokvera.org";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
    };

    private readonly HttpClient _httpClient;

    public TokveraClient(string? apiKey, string? baseUrl = null, HttpClient? httpClient = null)
    {
        ApiKey = apiKey;
        BaseUrl = NormalizeBaseUrl(baseUrl);
        _httpClient = httpClient ?? new HttpClient();
    }

    public string? ApiKey { get; }

    public string BaseUrl { get; }

    public async Task IngestEventAsync(object payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new ArgumentException("Tokvera API key is required", nameof(ApiKey));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1/events")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions)),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ApiKey);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new IOException($"Tokvera ingest failed with status {(int)response.StatusCode}: {body}");
        }
    }

    public static string NormalizeBaseUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DefaultBaseUrl;
        }

        var normalized = value.Trim().Trim('"', '\'');
        if (normalized.EndsWith("/v1/events", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^"/v1/events".Length];
        }

        while (normalized.EndsWith('/'))
        {
            normalized = normalized[..^1];
        }

        return normalized;
    }
}
