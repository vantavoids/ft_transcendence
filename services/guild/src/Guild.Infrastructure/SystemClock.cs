using Guild.Application.Abstractions;

namespace Guild.Infrastructure;

internal sealed class SystemClock : IClock
{
	public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
