using Chat.Application.Abstractions;

namespace Chat.Infrastructure;

internal sealed class SystemClock : IClock
{
	public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
