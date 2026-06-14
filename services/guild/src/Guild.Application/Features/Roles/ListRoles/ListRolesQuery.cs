using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Roles.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Roles.ListRoles;

public sealed record ListRolesQuery(long GuildId) : IQuery<Result<RoleListResponse>>;

/// <summary>
/// wrapped in a record so the generic <c>IQueryHandler&lt;,&gt;</c> constraint
/// (<c>TResponse : class</c>) is satisfied; the endpoint unwraps to return
/// <see cref="Items"/> as a flat JSON array per the contract
/// </summary>
public sealed record RoleListResponse(IReadOnlyList<RoleResponse> Items);
