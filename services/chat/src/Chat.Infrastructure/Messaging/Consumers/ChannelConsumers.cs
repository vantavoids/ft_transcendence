using Chat.Application.Abstractions;
using Chat.Application.Features.Channels.Common;
using Chat.Infrastructure.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Chat.Infrastructure.Messaging.Consumers;

// guild channel was created/updated/deleted. Guild resolves the members who may
// read the channel (EligibleUserIds) so we push the change only to them, keeping
// private channels invisible to members without read access.

public sealed class GuildChannelCreatedConsumer(
	IUserBroadcaster broadcaster,
	ILogger<GuildChannelCreatedConsumer> logger)
	: IConsumer<GuildChannelCreated>
{
	public async Task Consume(ConsumeContext<GuildChannelCreated> context)
	{
		var msg = context.Message;
		await broadcaster.BroadcastChannelCreatedAsync(
			msg.EligibleUserIds, ToDto(msg.Channel), context.CancellationToken);

		logger.LogDebug(
			"channel.created consumed: guild_id={GuildId} channel_id={ChannelId} eligible={Eligible}",
			msg.GuildId, msg.Channel.Id, msg.EligibleUserIds.Count);
	}

	internal static GuildChannelDto ToDto(ChannelPayload c) => new(
		c.Id, c.GuildId, c.CategoryId, c.Name, c.Topic, c.Type, c.Position, c.IsNsfw, c.SlowmodeSeconds);
}

public sealed class GuildChannelUpdatedConsumer(
	IUserBroadcaster broadcaster,
	ILogger<GuildChannelUpdatedConsumer> logger)
	: IConsumer<GuildChannelUpdated>
{
	public async Task Consume(ConsumeContext<GuildChannelUpdated> context)
	{
		var msg = context.Message;
		await broadcaster.BroadcastChannelUpdatedAsync(
			msg.EligibleUserIds, GuildChannelCreatedConsumer.ToDto(msg.Channel), context.CancellationToken);

		logger.LogDebug(
			"channel.updated consumed: guild_id={GuildId} channel_id={ChannelId} eligible={Eligible}",
			msg.GuildId, msg.Channel.Id, msg.EligibleUserIds.Count);
	}
}

public sealed class GuildChannelDeletedConsumer(
	IUserBroadcaster broadcaster,
	ILogger<GuildChannelDeletedConsumer> logger)
	: IConsumer<GuildChannelDeleted>
{
	public async Task Consume(ConsumeContext<GuildChannelDeleted> context)
	{
		var msg = context.Message;
		await broadcaster.BroadcastChannelDeletedAsync(
			msg.EligibleUserIds, msg.GuildId, msg.ChannelId, context.CancellationToken);

		logger.LogDebug(
			"channel.deleted consumed: guild_id={GuildId} channel_id={ChannelId} eligible={Eligible}",
			msg.GuildId, msg.ChannelId, msg.EligibleUserIds.Count);
	}
}
