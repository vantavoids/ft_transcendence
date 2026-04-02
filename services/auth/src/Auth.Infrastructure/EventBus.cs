using Auth.Application.Abstractions;
using MassTransit;

namespace Auth.Infrastructure.Messaging;

internal sealed class EventBus(IPublishEndpoint publishEndpoint) : IEventBus
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        return publishEndpoint.Publish(message, cancellationToken);
    }
}