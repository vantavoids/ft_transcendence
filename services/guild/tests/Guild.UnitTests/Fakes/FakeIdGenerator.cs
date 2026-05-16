using Guild.Application.Abstractions;

namespace Guild.UnitTests.Fakes;

internal sealed class FakeIdGenerator : IIdGenerator
{
	private long _current;

	public FakeIdGenerator(long seed = 1_000_000_000_000_000_000L)
	{
		_current = seed;
	}

	public long NextId() => ++_current;
}
