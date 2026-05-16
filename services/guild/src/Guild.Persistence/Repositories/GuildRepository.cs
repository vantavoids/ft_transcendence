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
		return context.Guilds
			.Include(g => g.Roles)
			.Include(g => g.Members)
			.Include(g => g.MemberRoles)
			.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
	}

	public Task<int> CountMembersAsync(long guildId, CancellationToken cancellationToken = default)
	{
		return context.Members
			.CountAsync(m => m.GuildId == guildId, cancellationToken);
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

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return context.SaveChangesAsync(cancellationToken);
	}
}
