using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tokvera.Tests;

internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    public List<JsonObject> Requests { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var payload = await request.Content!.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        Requests.Add(JsonNode.Parse(payload)!.AsObject());
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"ok\":true}", Encoding.UTF8, "application/json"),
        };
    }
}
