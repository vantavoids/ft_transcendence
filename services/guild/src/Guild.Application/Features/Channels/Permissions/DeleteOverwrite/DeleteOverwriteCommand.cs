using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.Permissions.DeleteOverwrite;

public sealed record DeleteOverwriteCommand(
	long ChannelId,
	long TargetId) : ICommand<Result>;
