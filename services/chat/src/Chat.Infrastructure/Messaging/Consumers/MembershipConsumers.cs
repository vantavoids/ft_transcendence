using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Persistence;
using Chat.Infrastructure.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Chat.Infrastructure.Messaging.Consumers;

public sealed class GuildMemberJoinedConsumer(
	IUserBroadcaster broadcaster,
	ILogger<GuildMemberJoinedConsumer> logger): IConsumer<GuildMemberJoined>
{
	public async Task Consume(ConsumeContext<GuildMemberJoined> context)
	{
		var msg = context.Message;

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
	ILogger<GuildMemberLeftConsumer> logger): IConsumer<GuildMemberLeft>
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

		await broadcaster.BroadcastGuildLeftAsync(
			userId: msg.UserId,
			guildId: msg.GuildId,
			ct: context.CancellationToken);

		logger.LogDebug(
			"guild.member_left consumed: guild_id={GuildId} user_id={UserId} evicted_subscriptions={Evicted}",
			msg.GuildId, msg.UserId, evicted);
	}
}

public sealed class UserLoggedOutConsumer(
	IUserBroadcaster broadcaster,
	ILogger<UserLoggedOutConsumer> logger): IConsumer<UserLoggedOut>
{
	public async Task Consume(ConsumeContext<UserLoggedOut> context)
	{
		var msg = context.Message;

		var evicted = await broadcaster.EvictUserFromActiveChannels(msg.UserId, context.CancellationToken);
		
		logger.LogDebug(
			"user.logged_out consumed: user_id={UserId} evicted_subscriptions={Evicted}"
			, msg.UserId, evicted);
	}
}

public sealed class UserDeletedConsumer(
	IUserBroadcaster broadcaster,
	IMessageRepository message,
	ILogger<UserDeletedConsumer> logger): IConsumer<UserDeleted>
{
	public async Task Consume(ConsumeContext<UserDeleted> context)
	{
		var msg = context.Message;

		var evicted = await broadcaster.EvictUserFromActiveChannels(msg.UserId, context.CancellationToken);

		await message.DeleteConversationAsync(msg.UserId, context.CancellationToken);
		// TODO: delete channel_read_states, and dm_unread_counts

		logger.LogDebug(
			"user.deleted consumed: user_id={UserId} evicted_subscriptions={Evicted}"
			, msg.UserId, evicted);
	}
}
