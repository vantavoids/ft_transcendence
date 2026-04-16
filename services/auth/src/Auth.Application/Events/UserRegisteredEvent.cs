using Auth.Application.Abstractions.Events;

namespace Auth.Application.Events;

public sealed record UserRegisteredEvent(long UserId, string Email) : IEvent
{
    public string EventType => "user.registered";
}