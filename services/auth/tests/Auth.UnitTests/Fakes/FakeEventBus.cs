using Auth.Application.Abstractions.Events;

namespace Auth.UnitTests.Fakes;

internal sealed class FakeEventBus : IEventBus
{
    private readonly List<object> _published = [];

    public IReadOnlyList<object> Published => _published;

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class, IEvent
    {
        _published.Add(message);
        return Task.CompletedTask;
    }
}
