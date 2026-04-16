using Chat.Application.Abstractions;
using MassTransit;

namespace Chat.Infrastructure.Messaging;

internal sealed class EventBus(IPublishEndpoint publishEndpoint) : IEventBus
{
	public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
		=> publishEndpoint.Publish(message, cancellationToken);
}
