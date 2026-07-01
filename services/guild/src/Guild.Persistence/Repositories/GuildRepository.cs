using Guild.Application.Abstractions.Persistence;
using Guild.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.Persistence.Repositories;

internal sealed class GuildRepository(GuildDbContext context) : IGuildRepository
{
	public Task<GuildEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
	{
		return context.Guilds
			.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
	}

	public Task<GuildEntity?> GetByIdWithMembershipAsync(long id, CancellationToken cancellationToken = default)
	{
		// AsSplitQuery: three collection Includes on one root would otherwise
		// produce a cartesian product (members x roles x member-roles) in a
		// single JOIN. split queries issue one SELECT per collection instead,
		// which EF 10 actively warns about when omitted here.
		return context.Guilds
			.Include(g => g.Roles)
			.Include(g => g.Members)
			.Include(g => g.MemberRoles)
			.AsSplitQuery()
			.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
	}

	public Task<GuildEntity?> GetByIdWithMembershipAsNoTrackingAsync(long id, CancellationToken cancellationToken = default)
	{
		// KNOWN TRADE-OFF: AsSplitQuery issues one SELECT per collection, not
		// wrapped in a single snapshot, so a membership change committing between
		// the Members and MemberRoles selects yields a momentarily inconsistent
		// read (e.g. member present but their role rows missing). this is an
		// eventual-consistency read endpoint and the skew is fail-safe -
		// permissions are under-evaluated, never over-granted - so we accept it
		// rather than forcing a REPEATABLE READ transaction on a hot read path.
		return context.Guilds
			.AsNoTracking()
			.Include(g => g.Roles)
			.Include(g => g.Members)
			.Include(g => g.MemberRoles)
			.AsSplitQuery()
			.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
	}

	public async Task<IReadOnlyList<MemberPage>> PageMembersAsync(
		long guildId, long? afterUserId, int limit, CancellationToken cancellationToken = default)
	{
		var membersQuery = context.Members
			.AsNoTracking()
			.Where(m => m.GuildId == guildId);
		if (afterUserId is { } cursor)
			membersQuery = membersQuery.Where(m => m.UserId > cursor);

		var members = await membersQuery
			.OrderBy(m => m.UserId)
			.Take(limit)
			.Select(m => new { m.UserId, m.Nickname, m.JoinedAt })
			.ToListAsync(cancellationToken);

		if (members.Count == 0)
			return [];

		// second bounded query: role ids for just the members on this page,
		// grouped in memory (at most `limit` members, not the whole guild).
		// KNOWN TRADE-OFF: this is a separate, non-snapshotted query, so a role
		// change committing between the two selects can leave a member listed with
		// stale RoleIds. acceptable eventual consistency for a paginated list; a
		// fresh page reflects the change.
		var userIds = members.Select(m => m.UserId).ToList();
		var assignments = await context.MemberRoles
			.AsNoTracking()
			.Where(mr => mr.GuildId == guildId && userIds.Contains(mr.UserId))
			.Select(mr => new { mr.UserId, mr.RoleId })
			.ToListAsync(cancellationToken);

		var rolesByUser = assignments
			.GroupBy(a => a.UserId)
			.ToDictionary(g => g.Key, g => (IReadOnlyList<long>)g.Select(a => a.RoleId).ToList());

		return members
			.Select(m => new MemberPage(
				m.UserId,
				m.Nickname,
				m.JoinedAt,
				rolesByUser.TryGetValue(m.UserId, out var ids) ? ids : []))
			.ToList();
	}

	public async Task<IReadOnlyList<MyGuildSummary>> ListForMemberAsync(
		long userId, CancellationToken cancellationToken = default)
	{
		// join the caller's membership rows to their guilds and project the summary
		// shape in the database. member_count is a correlated COUNT rather than a
		// loaded Members collection, so no aggregate is hydrated. joined_at comes
		// from the caller's own guild_members row.
		return await context.Members
			.AsNoTracking()
			.Where(m => m.UserId == userId)
			.Join(
				context.Guilds,
				m => m.GuildId,
				g => g.Id,
				(m, g) => new MyGuildSummary(
					g.Id,
					g.Name,
					g.IconUrl,
					g.OwnerId,
					context.Members.Count(x => x.GuildId == g.Id),
					m.JoinedAt))
			.ToListAsync(cancellationToken);
	}

	public Task<int> CountMembersAsync(long guildId, CancellationToken cancellationToken = default)
	{
		return context.Members
			.CountAsync(m => m.GuildId == guildId, cancellationToken);
	}

	public Task<int> CountOwnedByAsync(long userId, CancellationToken cancellationToken = default)
	{
		return context.Guilds
			.CountAsync(g => g.OwnerId == userId, cancellationToken);
	}

	public Task<bool> IsMemberAsync(long guildId, long userId, CancellationToken cancellationToken = default)
	{
		return context.Members
			.AnyAsync(m => m.GuildId == guildId && m.UserId == userId, cancellationToken);
	}

	public void Add(GuildEntity guild)
	{
		context.Guilds.Add(guild);
	}

	public void Update(GuildEntity guild)
	{
		context.Guilds.Update(guild);
	}

	public void Remove(GuildEntity guild)
	{
		context.Guilds.Remove(guild);
	}
}
