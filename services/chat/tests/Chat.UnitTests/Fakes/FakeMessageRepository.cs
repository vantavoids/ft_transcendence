using Chat.Application.Abstractions.Persistence;
using Chat.Domain.Messages;

namespace Chat.UnitTests.Fakes;

public sealed class FakeMessageRepository : IMessageRepository
{
	private readonly List<Message> _saved = [];

	public IReadOnlyList<Message> Saved => _saved;

	public void Reset() => _saved.Clear();

	public Task AddAsync(Message message, CancellationToken ct)
	{
		_saved.Add(message);
		return Task.CompletedTask;
	}
}
