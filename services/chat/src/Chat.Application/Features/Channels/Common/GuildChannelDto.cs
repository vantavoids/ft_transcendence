namespace Chat.Application.Features.Channels.Common;

/// <summary>
/// client-facing shape of a guild channel, pushed over SignalR when a channel
/// is created / updated / deleted so subscribers update their sidebar without a
/// refetch. mirrors Guild's channel wire shape; snowflake ids are quoted
/// strings.
/// </summary>
public sealed record GuildChannelDto(
	string Id,
	string GuildId,
	string? CategoryId,
	string Name,
	string? Topic,
	string Type,
	int Position,
	bool IsNsfw,
	int SlowmodeSeconds);
