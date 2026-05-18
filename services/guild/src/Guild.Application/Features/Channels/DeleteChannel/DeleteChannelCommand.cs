using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.DeleteChannel;

public sealed record DeleteChannelCommand(
	long GuildId,
	long ChannelId) : ICommand<Result>;
