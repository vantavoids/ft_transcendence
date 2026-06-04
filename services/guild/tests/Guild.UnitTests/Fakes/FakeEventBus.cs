using Guild.Application.Abstractions;

namespace Guild.UnitTests.Fakes;

internal sealed class FakeEventBus : IEventBus
{
	private readonly List<object> _published = [];

	public IReadOnlyList<object> Published => _published;

	public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
	{
		_published.Add(message);
		return Task.CompletedTask;
	}

	public T Single<T>() where T : class
	{
		return _published.OfType<T>().Single();
	}

	public bool Has<T>() where T : class
	{
		return _published.OfType<T>().Any();
	}
}
