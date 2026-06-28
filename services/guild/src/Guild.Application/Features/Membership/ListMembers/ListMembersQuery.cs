using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Membership.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.ListMembers;

public sealed record ListMembersQuery(long GuildId, long? After, int Limit) : IQuery<Result<IReadOnlyList<MemberResponse>>>;

