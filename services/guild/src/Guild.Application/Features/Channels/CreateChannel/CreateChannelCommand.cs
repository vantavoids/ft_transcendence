using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Channels.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.CreateChannel;

public sealed record CreateChannelCommand(
	long GuildId,
	string? Name,
	string? Type,
	long? CategoryId,
	string? Topic,
	int? Position,
	bool? IsNsfw = null,
	int? SlowmodeSeconds = null,
	IReadOnlyList<ChannelOverwriteInput>? Overwrites = null) : ICommand<Result<ChannelResponse>>;

/// <summary>
/// an allow/deny overwrite to apply atomically as the channel is created, so it
/// spawns with the intended per-role/per-member permissions instead of being
/// briefly world-readable while overwrites are PUT afterwards. mirrors the shape
/// of <c>PutOverwriteCommand</c>.
/// </summary>
public sealed record ChannelOverwriteInput(
	long TargetId,
	string? TargetType,
	long Allow,
	long Deny);
