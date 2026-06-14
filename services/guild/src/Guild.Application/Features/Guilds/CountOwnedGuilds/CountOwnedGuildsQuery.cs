using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Guilds.CountOwnedGuilds;

public sealed record CountOwnedGuildsQuery(long UserId) : IQuery<Result<OwnedGuildsCountResponse>>;

public sealed record OwnedGuildsCountResponse(int Count);
