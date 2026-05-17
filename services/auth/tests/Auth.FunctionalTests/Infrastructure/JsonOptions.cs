using System.Text.Json;
using System.Text.Json.Nodes;

namespace Auth.FunctionalTests.Infrastructure;

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions SnakeCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
}

internal static class HttpContentExtensions
{
    public static async Task<JsonNode> ReadJsonAsync(this HttpResponseMessage resp)
    {
        var json = await resp.Content.ReadAsStringAsync();
        return JsonNode.Parse(json)!;
    }
}
