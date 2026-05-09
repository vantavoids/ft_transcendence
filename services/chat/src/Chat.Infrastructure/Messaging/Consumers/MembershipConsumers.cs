using Chat.Application.Contracts;
using MassTransit;

namespace Chat.Infrastructure.Messaging.Consumers;

public sealed class GuildMemberJoinedConsumer : IConsumer<GuildMemberJoined>
{
	public Task Consume(ConsumeContext<GuildMemberJoined> context) => Task.CompletedTask;
}

public sealed class GuildMemberLeftConsumer : IConsumer<GuildMemberLeft>
{
	public Task Consume(ConsumeContext<GuildMemberLeft> context) => Task.CompletedTask;
}

public sealed class UserLoggedOutConsumer : IConsumer<UserLoggedOut>
{
	public Task Consume(ConsumeContext<UserLoggedOut> context) => Task.CompletedTask;
}
