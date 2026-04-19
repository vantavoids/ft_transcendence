namespace Auth.Application.Abstractions.Events;

public interface IEvent
{
    string EventType { get; }
}
