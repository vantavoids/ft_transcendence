using Chat.Application.Abstractions;

namespace Chat.UnitTests.Fakes;

public sealed class FakeIdGenerator : ISnowflakeIdGenerator
{
	private long _current;

	public FakeIdGenerator(long seed = 1_000_000_000_000_000_000L)
	{
		_current = seed;
	}

	public long NextId() => ++_current;
}
