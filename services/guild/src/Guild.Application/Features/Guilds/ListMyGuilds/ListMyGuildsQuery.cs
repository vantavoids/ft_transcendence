using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Guilds.ListMyGuilds;

public sealed record ListMyGuildsQuery : IQuery<Result<MyGuildListResponse>>;

/// <summary>
/// wrapped in a record so the generic <c>IQueryHandler&lt;,&gt;</c> constraint
/// (<c>TResponse : class</c>) is satisfied; serialised by Carter as a JSON
/// array via <see cref="Items"/>
/// </summary>
public sealed record MyGuildListResponse(IReadOnlyList<MyGuildDto> Items);
