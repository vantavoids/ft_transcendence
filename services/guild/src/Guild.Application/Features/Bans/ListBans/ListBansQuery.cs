using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Bans.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Bans.ListBans;

public sealed record ListBansQuery(long GuildId, long? After, int Limit) : IQuery<Result<IReadOnlyList<BanResponse>>>;

