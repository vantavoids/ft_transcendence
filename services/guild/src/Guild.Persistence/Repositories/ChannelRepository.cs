using Guild.Application.Abstractions.Persistence;
using Guild.Domain.Guild;
using Guild.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace Guild.Persistence.Repositories;

internal sealed class ChannelRepository(GuildDbContext context) : IChannelRepository
{
	public Task<Channel?> GetByIdAsync(long channelId, CancellationToken cancellationToken = default)
	{
		return context.Channels
			.FirstOrDefaultAsync(c => c.Id == channelId, cancellationToken);
	}

	public async Task<IReadOnlyList<Channel>> GetByGuildAsync(long guildId, CancellationToken cancellationToken = default)
	{
		return await context.Channels
			.Where(c => c.GuildId == guildId)
			.OrderBy(c => c.Position)
			.ToListAsync(cancellationToken);
	}

	public Task<int?> GetMaxPositionAsync(
		long guildId,
		long? categoryId,
		CancellationToken cancellationToken = default)
	{
		return context.Channels
			.Where(c => c.GuildId == guildId && c.CategoryId == categoryId)
			.Select(c => (int?)c.Position)
			.MaxAsync(cancellationToken);
	}

	public void Add(Channel channel)
	{
		context.Channels.Add(channel);
	}

	public void Update(Channel channel)
	{
		context.Channels.Update(channel);
	}

	public void Remove(Channel channel)
	{
		context.Channels.Remove(channel);
	}
}
