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
		// grouped in memory (at most `limit` members, not the whole guild)
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

	public async Task AddAsync(GuildEntity guild, CancellationToken cancellationToken = default)
	{
		await context.Guilds.AddAsync(guild, cancellationToken);
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
