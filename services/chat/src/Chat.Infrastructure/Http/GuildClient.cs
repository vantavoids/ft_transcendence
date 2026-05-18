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

	public Task<ChannelMembership?> GetMembershipAsync(long channelId, long userId, CancellationToken ct) =>
		http.GetFromJsonAsync<ChannelMembership>(
			$"channels/{channelId}/membership?user_id={userId}", JsonOptions, ct);
}
