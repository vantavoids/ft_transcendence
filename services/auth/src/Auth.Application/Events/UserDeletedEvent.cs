using Auth.Application.Abstractions.Events;

namespace Auth.Application.Events;

public sealed record UserDeletedEvent(long UserId) : IEvent
{
    public string EventType => "user.deleted";
}
