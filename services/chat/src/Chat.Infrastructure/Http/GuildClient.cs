using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Chat.Application.Abstractions;

namespace Chat.Infrastructure.Http;

internal sealed class GuildClient(HttpClient http) : IGuildClient
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
	};

	public async Task<ChannelMembership?> GetMembershipAsync(long channelId, long userId, CancellationToken ct)
	{
		try
		{
			return await http.GetFromJsonAsync<ChannelMembership>(
				$"channels/{channelId}/membership?user_id={userId}", JsonOptions, ct);
		}
		// GetFromJsonAsync throws on non-2xx; map 404 to null so the handler's
		// `membership is null -> ChannelNotFound` branch fires as intended
		catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}
	}

	public async Task<IReadOnlyList<long>> GetVisibleChannelIdsAsync(long userId, CancellationToken ct)
	{
		var channels = await http.GetFromJsonAsync<List<ChannelSummary>>($"users/{userId}/channels", JsonOptions, ct);
		return channels?.Select(c => long.Parse(c.Id)).ToList() ?? [];
	}

	private sealed record ChannelSummary(string Id);
}
