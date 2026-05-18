using Chat.Application.Abstractions;

namespace Chat.UnitTests.Fakes;

public sealed class FakeClock : IClock
{
	private DateTimeOffset _now;

	public FakeClock()
		: this(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))
	{
	}

	public FakeClock(DateTimeOffset anchor)
	{
		_now = anchor;
	}

	public DateTimeOffset UtcNow => _now;

	public void Set(DateTimeOffset value) => _now = value;

	public void Advance(TimeSpan delta) => _now = _now.Add(delta);
}
