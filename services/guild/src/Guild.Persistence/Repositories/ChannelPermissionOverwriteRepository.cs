using Guild.Application.Abstractions.Persistence;
using Guild.Domain.Guild;
using Guild.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace Guild.Persistence.Repositories;

internal sealed class ChannelPermissionOverwriteRepository(GuildDbContext context) : IChannelPermissionOverwriteRepository
{
	public async Task<IReadOnlyList<ChannelPermissionOverwrite>> GetForChannelAsync(
		long channelId,
		CancellationToken cancellationToken = default)
	{
		return await context.ChannelPermissionOverwrites
			.Where(o => o.ChannelId == channelId)
			.ToListAsync(cancellationToken);
	}

	public Task<ChannelPermissionOverwrite?> GetForChannelAndTargetAsync(
		long channelId,
		OverwriteTargetType targetType,
		long targetId,
		CancellationToken cancellationToken = default)
	{
		return context.ChannelPermissionOverwrites
			.FirstOrDefaultAsync(
				o => o.ChannelId == channelId
					&& o.TargetType == targetType
					&& o.TargetId == targetId,
				cancellationToken);
	}

	public Task<ChannelPermissionOverwrite?> GetForChannelByTargetIdAsync(
		long channelId,
		long targetId,
		CancellationToken cancellationToken = default)
	{
		return context.ChannelPermissionOverwrites
			.FirstOrDefaultAsync(
				o => o.ChannelId == channelId && o.TargetId == targetId,
				cancellationToken);
	}

	public Task RemoveAllForMemberAsync(long userId, CancellationToken cancellationToken = default)
	{
		// constrain on target type so a role overwrite sharing the id space is never touched
		return context.ChannelPermissionOverwrites
			.Where(o => o.TargetType == OverwriteTargetType.Member && o.TargetId == userId)
			.ExecuteDeleteAsync(cancellationToken);
	}

	public void Add(ChannelPermissionOverwrite overwrite)
	{
		context.ChannelPermissionOverwrites.Add(overwrite);
	}

	public void Update(ChannelPermissionOverwrite overwrite)
	{
		context.ChannelPermissionOverwrites.Update(overwrite);
	}

	public void Remove(ChannelPermissionOverwrite overwrite)
	{
		context.ChannelPermissionOverwrites.Remove(overwrite);
	}
}
