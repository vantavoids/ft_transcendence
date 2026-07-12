using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Persistence;
using Chat.Infrastructure.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Chat.Infrastructure.Messaging.Consumers;

public sealed class GuildMemberJoinedConsumer(
	IUserBroadcaster broadcaster,
	ILogger<GuildMemberJoinedConsumer> logger)
	: IConsumer<GuildMemberJoined>
{
	public async Task Consume(ConsumeContext<GuildMemberJoined> context)
	{
		var msg = context.Message;

		// subscribe the (already-connected) member's connections to the guild
		// group so they start receiving that guild's structure/presence broadcasts
		// without reconnecting. connect-time subscription is handled by the hub.
		await broadcaster.AddUserToGuildGroupAsync(msg.UserId, msg.GuildId, context.CancellationToken);

		// tell the rest of the guild's members (the guild group) that the roster
		// gained a member, so their member list updates without a refresh.
		await broadcaster.BroadcastMemberJoinedAsync(msg.GuildId, msg.UserId, context.CancellationToken);

		await broadcaster.BroadcastGuildJoinedAsync(
			userId: msg.UserId,
			guildId: msg.GuildId,
			guildName: msg.GuildName,
			ct: context.CancellationToken);

		logger.LogDebug(
			"guild.member_joined consumed: guild_id={GuildId} guild_name={GuildName} user_id={UserId}",
			msg.GuildId, msg.GuildName, msg.UserId);
	}
}

public sealed class GuildMemberLeftConsumer(
	IUserBroadcaster broadcaster,
	ILogger<GuildMemberLeftConsumer> logger)
	: IConsumer<GuildMemberLeft>
{
	public async Task Consume(ConsumeContext<GuildMemberLeft> context)
	{
		var msg = context.Message;

		// pull the user's connections out of the guild's channel groups *before*
		// notifying the client, so a kicked/banned member stops receiving channel
		// broadcasts server-side even if their client never calls LeaveChannel.
		var evicted = await broadcaster.EvictFromGuildChannelsAsync(
			userId: msg.UserId,
			guildId: msg.GuildId,
			ct: context.CancellationToken);

		// tell the remaining members the roster shrank (while the leaver is still
		// in the group is fine; they also get GuildLeft below), then drop the
		// leaver from the guild group so they stop receiving its broadcasts
		// server-side even if the client lingers.
		await broadcaster.BroadcastMemberLeftAsync(msg.GuildId, msg.UserId, context.CancellationToken);
		await broadcaster.RemoveUserFromGuildGroupAsync(msg.UserId, msg.GuildId, context.CancellationToken);

		await broadcaster.BroadcastGuildLeftAsync(
			userId: msg.UserId,
			guildId: msg.GuildId,
			ct: context.CancellationToken);

		logger.LogDebug(
			"guild.member_left consumed: guild_id={GuildId} user_id={UserId} evicted_subscriptions={Evicted}",
			msg.GuildId, msg.UserId, evicted);
	}
}

public sealed class GuildDeletedConsumer(
	IUserBroadcaster broadcaster,
	IMessageRepository messages,
	ILogger<GuildDeletedConsumer> logger)
	: IConsumer<GuildDeleted>
{
	public async Task Consume(ConsumeContext<GuildDeleted> context)
	{
		var msg = context.Message;

		// purge each deleted channel's message history so it does not outlive the
		// guild. the ids come on the event because Chat cannot read Guild's DB.
		foreach (var channelId in msg.ChannelIds)
			await messages.DeleteChannelMessagesAsync(channelId, context.CancellationToken);

		// notify every connected member (the guild group) that the guild is gone
		// so their client drops it live. server-side group membership becomes
		// stale but harmless: no further broadcasts target a deleted guild.
		await broadcaster.BroadcastGuildDeletedAsync(msg.GuildId, context.CancellationToken);

		logger.LogDebug(
			"guild.deleted consumed: guild_id={GuildId} purged_channels={PurgedChannels}",
			msg.GuildId, msg.ChannelIds.Count);
	}
}

public sealed class GuildUpdatedConsumer(
	IUserBroadcaster broadcaster,
	ILogger<GuildUpdatedConsumer> logger)
	: IConsumer<GuildUpdated>
{
	public async Task Consume(ConsumeContext<GuildUpdated> context)
	{
		var msg = context.Message;
		await broadcaster.BroadcastGuildUpdatedAsync(msg.GuildId, msg.Name, msg.IconUrl, context.CancellationToken);
		logger.LogDebug("guild.updated consumed: guild_id={GuildId}", msg.GuildId);
	}
}

public sealed class GuildRolesChangedConsumer(
	IUserBroadcaster broadcaster,
	ILogger<GuildRolesChangedConsumer> logger)
	: IConsumer<GuildRolesChanged>
{
	public async Task Consume(ConsumeContext<GuildRolesChanged> context)
	{
		var msg = context.Message;
		await broadcaster.BroadcastRolesChangedAsync(msg.GuildId, context.CancellationToken);
		logger.LogDebug("guild.roles_changed consumed: guild_id={GuildId}", msg.GuildId);
	}
}

public sealed class GuildMemberUpdatedConsumer(
	IUserBroadcaster broadcaster,
	ILogger<GuildMemberUpdatedConsumer> logger)
	: IConsumer<GuildMemberUpdated>
{
	public async Task Consume(ConsumeContext<GuildMemberUpdated> context)
	{
		var msg = context.Message;
		await broadcaster.BroadcastMemberUpdatedAsync(msg.GuildId, msg.UserId, context.CancellationToken);
		logger.LogDebug("guild.member_updated consumed: guild_id={GuildId} user_id={UserId}", msg.GuildId, msg.UserId);
	}
}

public sealed class ChannelAccessRevokedConsumer(
	IUserBroadcaster broadcaster,
	ILogger<ChannelAccessRevokedConsumer> logger)
	: IConsumer<ChannelAccessRevoked>
{
	public async Task Consume(ConsumeContext<ChannelAccessRevoked> context)
	{
		var msg = context.Message;

		// read access to a single channel was revoked (role or overwrite change)
		// without the member leaving the guild. purge their connections from just
		// that channel's group so they stop receiving broadcasts server-side even
		// if their client never unsubscribes.
		var evicted = await broadcaster.EvictFromChannelAsync(
			userId: msg.UserId,
			channelId: msg.ChannelId,
			ct: context.CancellationToken);

		logger.LogDebug(
			"channel.access_revoked consumed: channel_id={ChannelId} user_id={UserId} evicted_subscriptions={Evicted}",
			msg.ChannelId, msg.UserId, evicted);
	}
}

public sealed class UserLoggedOutConsumer(
	IUserBroadcaster broadcaster,
	ILogger<UserLoggedOutConsumer> logger): IConsumer<UserLoggedOut>
{
	public async Task Consume(ConsumeContext<UserLoggedOut> context)
	{
		var msg = context.Message;

		var disconnected = await broadcaster.DisconnectUserAsync(msg.UserId, context.CancellationToken);

		logger.LogDebug(
			"user.logged_out consumed: user_id={UserId} disconnected_connections={Disconnected}"
			, msg.UserId, disconnected);
	}
}

public sealed class UserDeletedConsumer(
	IUserBroadcaster broadcaster,
	IMessageRepository messages,
	IReadStateRepository readStates,
	ILogger<UserDeletedConsumer> logger): IConsumer<UserDeleted>
{
	public async Task Consume(ConsumeContext<UserDeleted> context)
	{
		var msg = context.Message;

		var disconnected = await broadcaster.DisconnectUserAsync(msg.UserId, context.CancellationToken);

		await messages.DeleteConversationAsync(msg.UserId, context.CancellationToken);
		await readStates.DeleteAllForUserAsync(msg.UserId, context.CancellationToken);

		logger.LogDebug(
			"user.deleted consumed: user_id={UserId} disconnected_connections={Disconnected}"
			, msg.UserId, disconnected);
	}
}
