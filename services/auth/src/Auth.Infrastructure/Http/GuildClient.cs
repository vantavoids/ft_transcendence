using System.Net.Http.Json;
using System.Text.Json;
using Auth.Application.Abstractions;

namespace Auth.Infrastructure.Http;

internal sealed class GuildClient(HttpClient http) : IGuildClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task<int> GetOwnedGuildsCountAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync(
            $"users/{userId}/owned-guilds-count", cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<OwnedGuildsCountPayload>(
            JsonOptions, cancellationToken);

        return payload?.Count ?? 0;
    }

    private sealed record OwnedGuildsCountPayload(int Count);
}
