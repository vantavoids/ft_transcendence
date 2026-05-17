using Guild.Application.Abstractions.Persistence;
using Guild.Domain.Guild;
using Guild.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace Guild.Persistence.Repositories;

internal sealed class ChannelCategoryRepository(GuildDbContext context) : IChannelCategoryRepository
{
	public Task<ChannelCategory?> GetByIdAsync(
		long guildId,
		long categoryId,
		CancellationToken cancellationToken = default)
	{
		return context.ChannelCategories
			.FirstOrDefaultAsync(c => c.GuildId == guildId && c.Id == categoryId, cancellationToken);
	}

	public async Task<int?> GetMaxPositionAsync(long guildId, CancellationToken cancellationToken = default)
	{
		var hasAny = await context.ChannelCategories
			.AnyAsync(c => c.GuildId == guildId, cancellationToken);
		if (!hasAny)
			return null;

		return await context.ChannelCategories
			.Where(c => c.GuildId == guildId)
			.MaxAsync(c => c.Position, cancellationToken);
	}

	public async Task AddAsync(ChannelCategory category, CancellationToken cancellationToken = default)
	{
		await context.ChannelCategories.AddAsync(category, cancellationToken);
	}

	public void Update(ChannelCategory category)
	{
		context.ChannelCategories.Update(category);
	}

	public void Remove(ChannelCategory category)
	{
		context.ChannelCategories.Remove(category);
	}

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return context.SaveChangesAsync(cancellationToken);
	}
}
