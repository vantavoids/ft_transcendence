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
	int? Position) : ICommand<Result<ChannelResponse>>;
