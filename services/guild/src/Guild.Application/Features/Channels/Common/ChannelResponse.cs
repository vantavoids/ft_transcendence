using Guild.Domain.Guild;

namespace Guild.Application.Features.Channels.Common;

/// <summary>
/// wire shape for a Channel. snowflake IDs are emitted as quoted strings so
/// JS clients can keep them precise. <c>Type</c> is lowercase ("text"/"voice")
/// to match the contract
/// </summary>
public sealed record ChannelResponse(
	string Id,
	string GuildId,
	string? CategoryId,
	string Name,
	string? Topic,
	string Type,
	int Position)
{
	public static ChannelResponse From(Channel c) => new(
		Id: c.Id.ToString(),
		GuildId: c.GuildId.ToString(),
		CategoryId: c.CategoryId?.ToString(),
		Name: c.Name,
		Topic: c.Topic,
		Type: c.Type.ToString().ToLowerInvariant(),
		Position: c.Position);
}
