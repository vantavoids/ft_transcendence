using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Guilds.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Guilds.TransferOwnership;

public sealed record TransferOwnershipCommand(
	long GuildId,
	long NewOwnerId) : ICommand<Result<GuildDto>>;
