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

// category lifecycle -> guild group (no per-member read restriction on categories).

public sealed class GuildCategoryCreatedConsumer(
	IUserBroadcaster broadcaster,
	ILogger<GuildCategoryCreatedConsumer> logger)
	: IConsumer<GuildCategoryCreated>
{
	public async Task Consume(ConsumeContext<GuildCategoryCreated> context)
	{
		var msg = context.Message;
		await broadcaster.BroadcastCategoryCreatedAsync(msg.GuildId, ToDto(msg.Category), context.CancellationToken);
		logger.LogDebug("category.created consumed: guild_id={GuildId} category_id={CategoryId}", msg.GuildId, msg.Category.Id);
	}

	internal static GuildCategoryDto ToDto(CategoryPayload c) => new(c.Id, c.GuildId, c.Name, c.Position);
}

public sealed class GuildCategoryUpdatedConsumer(
	IUserBroadcaster broadcaster,
	ILogger<GuildCategoryUpdatedConsumer> logger)
	: IConsumer<GuildCategoryUpdated>
{
	public async Task Consume(ConsumeContext<GuildCategoryUpdated> context)
	{
		var msg = context.Message;
		await broadcaster.BroadcastCategoryUpdatedAsync(
			msg.GuildId, GuildCategoryCreatedConsumer.ToDto(msg.Category), context.CancellationToken);
		logger.LogDebug("category.updated consumed: guild_id={GuildId} category_id={CategoryId}", msg.GuildId, msg.Category.Id);
	}
}

public sealed class GuildCategoryDeletedConsumer(
	IUserBroadcaster broadcaster,
	ILogger<GuildCategoryDeletedConsumer> logger)
	: IConsumer<GuildCategoryDeleted>
{
	public async Task Consume(ConsumeContext<GuildCategoryDeleted> context)
	{
		var msg = context.Message;
		await broadcaster.BroadcastCategoryDeletedAsync(msg.GuildId, msg.CategoryId, context.CancellationToken);
		logger.LogDebug("category.deleted consumed: guild_id={GuildId} category_id={CategoryId}", msg.GuildId, msg.CategoryId);
	}
}
