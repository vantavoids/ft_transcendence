using Guild.Application.Abstractions.Security;

namespace Guild.UnitTests.Fakes;

internal sealed class FakeCurrentUser : ICurrentUser
{
	public long Id { get; set; }
}
