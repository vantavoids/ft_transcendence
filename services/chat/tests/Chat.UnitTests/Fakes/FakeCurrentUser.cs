using Chat.Application.Abstractions.Authentication;

namespace Chat.UnitTests.Fakes;

public sealed class FakeCurrentUser : ICurrentUser
{
	public long UserId { get; set; }
}
