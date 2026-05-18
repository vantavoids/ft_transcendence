using Chat.Application.Abstractions;

namespace Chat.UnitTests.Fakes;

public sealed class FakeEventBus : IEventBus
{
	private readonly List<object> _published = [];

	public IReadOnlyList<object> Published => _published;

	public IReadOnlyList<T> PublishedOf<T>() where T : class =>
		_published.OfType<T>().ToList();

	public void Reset() => _published.Clear();

	public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
	{
		_published.Add(message);
		return Task.CompletedTask;
	}
}
