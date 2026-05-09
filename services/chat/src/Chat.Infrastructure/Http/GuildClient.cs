using System.Net.Http.Json;
using Chat.Application.Abstractions;

namespace Chat.Infrastructure.Http;

internal sealed class GuildClient(HttpClient http) : IGuildClient
{
	public Task<ChannelMembership?> GetMembershipAsync(Guid channelId, Guid userId, CancellationToken ct) =>
		http.GetFromJsonAsync<ChannelMembership>(
			$"channels/{channelId}/membership?user_id={userId}", ct);
}
