using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Bans.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Bans.ListBans;

public sealed record ListBansQuery(long GuildId, long? After, int Limit) : IQuery<Result<BanListResponse>>;

/// <summary>
/// wrapped in a record so the generic <c>IQueryHandler&lt;,&gt;</c> constraint
/// (<c>TResponse : class</c>) is satisfied; the endpoint unwraps to return
/// <see cref="Items"/> as a flat JSON array per the contract
/// </summary>
public sealed record BanListResponse(IReadOnlyList<BanResponse> Items);
