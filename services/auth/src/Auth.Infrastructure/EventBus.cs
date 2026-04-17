using Auth.Application.Abstractions.Events;
using MassTransit;

namespace Auth.Infrastructure;

internal sealed class EventBus(IPublishEndpoint publishEndpoint) : IEventBus
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class, IEvent
    {
        return publishEndpoint.Publish(message, cancellationToken);
    }
}