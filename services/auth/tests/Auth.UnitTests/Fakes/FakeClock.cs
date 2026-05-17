using Auth.Application.Abstractions;

namespace Auth.UnitTests.Fakes;

internal sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
