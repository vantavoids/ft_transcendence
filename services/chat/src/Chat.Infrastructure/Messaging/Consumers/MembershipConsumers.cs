using Chat.Application.Abstractions;
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

public sealed class GuildMemberLeftConsumer(ILogger<GuildMemberLeftConsumer> logger)
	: IConsumer<GuildMemberLeft>
{
	public Task Consume(ConsumeContext<GuildMemberLeft> context)
	{
		var msg = context.Message;
		logger.LogDebug(
			"guild.member_left consumed: guild_id={GuildId} user_id={UserId}",
			msg.GuildId, msg.UserId);
		return Task.CompletedTask;
	}
}

public sealed class UserLoggedOutConsumer(ILogger<UserLoggedOutConsumer> logger)
	: IConsumer<UserLoggedOut>
{
	public Task Consume(ConsumeContext<UserLoggedOut> context)
	{
		var msg = context.Message;
		logger.LogDebug("user.logged_out consumed: user_id={UserId}", msg.UserId);
		return Task.CompletedTask;
	}
}
