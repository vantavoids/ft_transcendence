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
		var guild = await guilds.GetByIdWithMembershipAsync(query.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (guild.Members.All(m => m.UserId != currentUser.Id))
			return GuildFailures.NotAMember;

		var rolesByMember = guild.MemberRoles
			.GroupBy(mr => mr.UserId)
			.ToDictionary(g => g.Key, g => g.Select(mr => mr.RoleId.ToString()).ToList());

		var page = guild.Members
			.Where(m => query.After is null || m.UserId > query.After.Value)
			.OrderBy(m => m.UserId)
			.Take(query.Limit)
			.Select(m => new MemberResponse(
				UserId: m.UserId.ToString(),
				GuildId: guild.Id.ToString(),
				Nickname: m.Nickname,
				Roles: rolesByMember.TryGetValue(m.UserId, out var ids) ? ids : new List<string>(),
				JoinedAt: m.JoinedAt))
			.ToList();

		return new MemberListResponse(page);
	}
}
