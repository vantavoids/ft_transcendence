using Chat.Application.Abstractions;

namespace Chat.UnitTests.Fakes;

public sealed class FakeUserClient : IUserClient
{
	private readonly Dictionary<(long, long), UsersRelationship?> _map = new();

	public void Setup(long callerId, long recipientId, UsersRelationship? relationship)
		=> _map[(callerId, recipientId)] = relationship;

	public void Reset() => _map.Clear();

	public Task<UsersRelationship?> GetUsersRelationship(long callerId, long recipientId, CancellationToken ct)
		=> Task.FromResult(_map.TryGetValue((callerId, recipientId), out var rel) ? rel : null);
}
