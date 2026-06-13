using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Membership.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.ListMembers;

public sealed record ListMembersQuery(long GuildId, long? After, int Limit) : IQuery<Result<MemberListResponse>>;

/// <summary>
/// wrapped in a record so the generic <c>IQueryHandler&lt;,&gt;</c> constraint
/// (<c>TResponse : class</c>) is satisfied; the endpoint unwraps to return
/// <see cref="Items"/> as a flat JSON array per the contract
/// </summary>
public sealed record MemberListResponse(IReadOnlyList<MemberResponse> Items);
