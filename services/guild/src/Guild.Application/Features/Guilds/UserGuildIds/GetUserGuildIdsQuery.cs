using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Guilds.UserGuildIds;

public sealed record GetUserGuildIdsQuery(long UserId) : IQuery<Result<IReadOnlyList<long>>>;
