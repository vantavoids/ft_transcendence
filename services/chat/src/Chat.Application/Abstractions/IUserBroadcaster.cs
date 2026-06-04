namespace Chat.Application.Abstractions;

/// <summary>
/// pushes per-user real-time notifications over SignalR. impl lives in the
/// Presentation layer because <c>IHubContext</c> is an ASP.NET abstraction
/// that must not leak into Infrastructure
/// </summary>
public interface IUserBroadcaster
{
	Task BroadcastGuildJoinedAsync(long userId, long guildId, string guildName, CancellationToken ct);
}
