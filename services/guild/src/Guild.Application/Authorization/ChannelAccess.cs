using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Contracts;
using Guild.Domain.Guild;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.Application.Authorization;

// read-access captured before a mutation, diffed against the after-state by
// PublishAccessChangesAsync to emit revocations and grants. Candidates is the
// full (channel, user) set considered; Before is the subset readable pre-change.
internal readonly record struct ChannelReadSnapshot(
	HashSet<(long ChannelId, long UserId)> Before,
	IReadOnlyList<(long ChannelId, long UserId)> Candidates,
	IReadOnlyDictionary<long, IReadOnlyList<ChannelPermissionOverwrite>> Overwrites);

internal static class ChannelAccess
{
	// role mutations reach every channel (base perms apply everywhere) but leave
	// overwrites unchanged, so the captured set also feeds the after-diff.
	public static async Task<ChannelReadSnapshot> CaptureAsync(
		GuildEntity guild,
		IReadOnlyCollection<long> affectedUserIds,
		IChannelRepository channels,
		IChannelPermissionOverwriteRepository overwrites,
		CancellationToken cancellationToken)
	{
		var guildChannels = await channels.GetByGuildAsync(guild.Id, cancellationToken);
		var allOverwrites = await overwrites.GetForGuildAsync(guild.Id, cancellationToken);
		var byChannel = allOverwrites
			.GroupBy(o => o.ChannelId)
			.ToDictionary(g => g.Key, g => (IReadOnlyList<ChannelPermissionOverwrite>)g.ToList());

		return Capture(guild, Pairs(guildChannels.Select(c => c.Id), affectedUserIds), byChannel);
	}

	// overwrite mutations touch a single channel; the caller supplies the changed
	// after-overwrites when publishing.
	public static ChannelReadSnapshot CaptureForChannel(
		GuildEntity guild,
		long channelId,
		IEnumerable<long> affectedUserIds,
		IReadOnlyList<ChannelPermissionOverwrite> channelOverwrites) =>
		Capture(guild, Pairs([channelId], affectedUserIds), Group(channelId, channelOverwrites));

	// role mutations: overwrites are unchanged, so the snapshot's set is the after-state.
	public static Task PublishAccessChangesAsync(
		IEventBus eventBus, GuildEntity guild, ChannelReadSnapshot snapshot, CancellationToken cancellationToken) =>
		PublishAccessChangesAsync(eventBus, guild, snapshot, snapshot.Overwrites, cancellationToken);

	// overwrite mutations: one channel's overwrites changed.
	public static Task PublishAccessChangesAsync(
		IEventBus eventBus, GuildEntity guild, ChannelReadSnapshot snapshot,
		long channelId, IReadOnlyList<ChannelPermissionOverwrite> afterChannelOverwrites,
		CancellationToken cancellationToken) =>
		PublishAccessChangesAsync(eventBus, guild, snapshot, Group(channelId, afterChannelOverwrites), cancellationToken);

	// members whose effective permissions include ReadMessages for the channel,
	// given its current overwrites. feeds the eligible-user set on channel
	// lifecycle events so Chat can target only members who may see the channel.
	public static IReadOnlyList<long> ReadersOf(
		GuildEntity guild, long channelId, IReadOnlyList<ChannelPermissionOverwrite> channelOverwrites)
	{
		var byChannel = Group(channelId, channelOverwrites);
		var readers = new List<long>();
		foreach (var member in guild.Members)
		{
			if (CanRead(guild, channelId, member.UserId, byChannel))
				readers.Add(member.UserId);
		}
		return readers;
	}

	public static IEnumerable<long> MembersAffectedBy(
		GuildEntity guild, OverwriteTargetType targetType, long targetId)
	{
		if (targetType == OverwriteTargetType.Member)
			return [targetId];

		var role = guild.Roles.FirstOrDefault(r => r.Id == targetId);
		if (role is null)
			return [];

		return role.IsDefault
			? guild.Members.Select(m => m.UserId)
			: guild.MemberRoles.Where(mr => mr.RoleId == targetId).Select(mr => mr.UserId);
	}

	private static async Task PublishAccessChangesAsync(
		IEventBus eventBus, GuildEntity guild, ChannelReadSnapshot snapshot,
		IReadOnlyDictionary<long, IReadOnlyList<ChannelPermissionOverwrite>> afterOverwrites,
		CancellationToken cancellationToken)
	{
		// re-resolve each candidate once and publish only on a transition: a pair
		// readable before but not after is a revocation; not before but after is a
		// grant (the member gains a newly-visible channel).
		foreach (var (channelId, userId) in snapshot.Candidates)
		{
			var wasReadable = snapshot.Before.Contains((channelId, userId));
			var isReadable = CanRead(guild, channelId, userId, afterOverwrites);

			if (wasReadable && !isReadable)
				await eventBus.PublishAsync(new ChannelAccessRevoked(channelId, userId), cancellationToken);
			else if (!wasReadable && isReadable)
				await eventBus.PublishAsync(new ChannelAccessGranted(channelId, guild.Id, userId), cancellationToken);
		}
	}

	private static ChannelReadSnapshot Capture(
		GuildEntity guild,
		IEnumerable<(long ChannelId, long UserId)> candidates,
		IReadOnlyDictionary<long, IReadOnlyList<ChannelPermissionOverwrite>> overwrites)
	{
		var candidateList = candidates.ToList();
		return new(ReadableIn(guild, candidateList, overwrites), candidateList, overwrites);
	}

	private static HashSet<(long ChannelId, long UserId)> ReadableIn(
		GuildEntity guild,
		IEnumerable<(long ChannelId, long UserId)> candidates,
		IReadOnlyDictionary<long, IReadOnlyList<ChannelPermissionOverwrite>> overwritesByChannel)
	{
		var readable = new HashSet<(long, long)>();
		foreach (var (channelId, userId) in candidates)
		{
			if (CanRead(guild, channelId, userId, overwritesByChannel))
				readable.Add((channelId, userId));
		}

		return readable;
	}

	private static bool CanRead(
		GuildEntity guild, long channelId, long userId,
		IReadOnlyDictionary<long, IReadOnlyList<ChannelPermissionOverwrite>> overwritesByChannel)
	{
		var overwrites = overwritesByChannel.TryGetValue(channelId, out var list) ? list : [];
		return PermissionResolver.HasPermission(
			PermissionResolver.Resolve(guild, userId, overwrites), Permission.ReadMessages);
	}

	// lazy: role mutations on @everyone span (all channels x all members); streaming
	// avoids materializing the full cartesian product just to fold it into a set.
	private static IEnumerable<(long ChannelId, long UserId)> Pairs(
		IEnumerable<long> channelIds, IEnumerable<long> userIds) =>
		channelIds.SelectMany(c => userIds.Select(u => (c, u)));

	private static Dictionary<long, IReadOnlyList<ChannelPermissionOverwrite>> Group(
		long channelId, IReadOnlyList<ChannelPermissionOverwrite> overwrites) =>
		new() { [channelId] = overwrites };
}
