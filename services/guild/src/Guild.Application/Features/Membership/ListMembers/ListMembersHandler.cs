using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Features.Membership.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.ListMembers;

internal sealed class ListMembersHandler(
	IGuildRepository guilds,
	ICurrentUser currentUser)
	: IQueryHandler<ListMembersQuery, Result<MemberListResponse>>
{
	public async Task<Result<MemberListResponse>> HandleAsync(
		ListMembersQuery query,
		CancellationToken cancellationToken = default)
	{
		// lightweight gate: listing members never needs the full aggregate or a
		// permission, just "guild exists" + "caller is a member". the members
		// themselves are then keyset-paged straight from the DB, so a 10k-member
		// guild no longer hydrates every row to return one page.
		var guild = await guilds.GetByIdAsync(query.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (!await guilds.IsMemberAsync(query.GuildId, currentUser.Id, cancellationToken))
			return GuildFailures.NotAMember;

		var page = await guilds.PageMembersAsync(
			query.GuildId, query.After, query.Limit, cancellationToken);

		var items = page
			.Select(m => new MemberResponse(
				UserId: m.UserId.ToString(),
				GuildId: query.GuildId.ToString(),
				Nickname: m.Nickname,
				Roles: m.RoleIds.Select(id => id.ToString()).ToList(),
				JoinedAt: m.JoinedAt))
			.ToList();

		return new MemberListResponse(items);
	}
}
