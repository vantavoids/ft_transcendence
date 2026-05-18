using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Channels.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.UpdateChannel;

/// <summary>
/// patchable fields are nullable; <see cref="CategoryIdProvided"/> distinguishes
/// "no change" (null) from "set to null" since both serialise as null on the wire
/// </summary>
public sealed record UpdateChannelCommand(
	long GuildId,
	long ChannelId,
	string? Name,
	string? Topic,
	int? Position,
	long? CategoryId,
	bool CategoryIdProvided,
	bool TopicProvided) : ICommand<Result<ChannelResponse>>;
