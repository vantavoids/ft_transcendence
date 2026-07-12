namespace Chat.Application.Features.Channels.Common;

/// <summary>
/// client-facing shape of a channel category, pushed over SignalR to the guild
/// group when a category is created / updated / deleted. snowflake ids are
/// quoted strings.
/// </summary>
public sealed record GuildCategoryDto(
	string Id,
	string GuildId,
	string Name,
	int Position);
