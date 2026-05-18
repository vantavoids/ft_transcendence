namespace Chat.Application.Abstractions;

public interface IClock
{
	DateTimeOffset UtcNow { get; }
}
