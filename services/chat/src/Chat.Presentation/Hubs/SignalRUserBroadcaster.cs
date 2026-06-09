using Chat.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

/// <summary>
/// routes per-user payloads to every connection the user has open using
/// SignalR's built-in user routing. resolution goes through
/// <c>DefaultUserIdProvider</c>, which reads <c>ClaimTypes.NameIdentifier</c>
/// off the JWT-authenticated identity (the user's snowflake string, since the
/// JwtBearer middleware auto-maps the <c>sub</c> claim to it)
/// </summary>
internal sealed class SignalRUserBroadcaster(IHubContext<ChatHub, IChatClient> hub)
	: IUserBroadcaster
{
	public Task BroadcastGuildJoinedAsync(long userId, long guildId, string guildName, CancellationToken ct) =>
		hub.Clients.User(userId.ToString()).GuildJoined(guildId.ToString(), guildName);

	public Task BroadcastGuildLeftAsync(long userId, long guildId, CancellationToken ct) =>
		hub.Clients.User(userId.ToString()).GuildLeft(guildId.ToString());
}
