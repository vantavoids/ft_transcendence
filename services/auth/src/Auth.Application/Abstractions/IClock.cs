namespace Auth.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
