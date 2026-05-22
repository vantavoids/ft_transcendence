using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.Permissions.PutOverwrite;

public sealed record PutOverwriteCommand(
	long ChannelId,
	long TargetId,
	string? TargetType,
	long Allow,
	long Deny) : ICommand<Result>;
