using Guild.Application.Abstractions.Persistence;
using Guild.Domain.Guild;
using Guild.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace Guild.Persistence.Repositories;

internal sealed class GuildInviteRepository(GuildDbContext context) : IGuildInviteRepository
{
	public Task<GuildInvite?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
	{
		return context.GuildInvites
			.FirstOrDefaultAsync(i => i.Code == code, cancellationToken);
	}

	public async Task<IReadOnlyList<GuildInvite>> ListByGuildAsync(long guildId, CancellationToken cancellationToken = default)
	{
		return await context.GuildInvites
			.Where(i => i.GuildId == guildId && !i.IsRevoked)
			.OrderByDescending(i => i.CreatedAt)
			.ToListAsync(cancellationToken);
	}

	public void Add(GuildInvite invite)
	{
		context.GuildInvites.Add(invite);
	}

	public void Update(GuildInvite invite)
	{
		context.GuildInvites.Update(invite);
	}

	public Task<int> DeleteRevokedAndExpiredAsync(DateTimeOffset expiredBefore, CancellationToken cancellationToken = default) => context.GuildInvites
		.Where(i => i.IsRevoked || (i.ExpiresAt != null && i.ExpiresAt < expiredBefore))
		.ExecuteDeleteAsync(cancellationToken);

	public Task RemoveAllByCreatorAsync(long creatorUserId, CancellationToken cancellationToken = default) => context.GuildInvites
		.Where(i => i.CreatedBy == creatorUserId)
		.ExecuteDeleteAsync(cancellationToken);
}
