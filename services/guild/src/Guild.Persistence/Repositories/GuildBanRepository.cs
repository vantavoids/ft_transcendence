using Guild.Application.Abstractions.Persistence;
using Guild.Domain.Guild;
using Guild.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace Guild.Persistence.Repositories;

internal sealed class GuildBanRepository(GuildDbContext context) : IGuildBanRepository
{
	public async Task<IReadOnlyList<GuildBan>> ListByGuildAsync(
		long guildId,
		long? afterUserId,
		int limit,
		CancellationToken cancellationToken = default)
	{
		var query = context.GuildBans.AsNoTracking().Where(b => b.GuildId == guildId);
		if (afterUserId is { } cursor)
			query = query.Where(b => b.UserId > cursor);

		return await query
			.OrderBy(b => b.UserId)
			.Take(limit)
			.ToListAsync(cancellationToken);
	}

	public Task<GuildBan?> FindAsync(long guildId, long userId, CancellationToken cancellationToken = default)
	{
		return context.GuildBans
			.FirstOrDefaultAsync(b => b.GuildId == guildId && b.UserId == userId, cancellationToken);
	}

	public async Task AddAsync(GuildBan ban, CancellationToken cancellationToken = default)
	{
		await context.GuildBans.AddAsync(ban, cancellationToken);
	}

	public void Remove(GuildBan ban)
	{
		context.GuildBans.Remove(ban);
	}

	public Task RemoveAllForUserAsync(long userId, CancellationToken cancellationToken = default)
	{
		return context.GuildBans
			.Where(b => b.UserId == userId)
			.ExecuteDeleteAsync(cancellationToken);
	}
}
