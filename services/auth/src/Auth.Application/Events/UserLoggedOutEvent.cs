using Auth.Application.Abstractions.Events;

namespace Auth.Application.Events;

public sealed record UserLoggedOutEvent(long UserId): IEvent
{
    public string EventType => "user.logged_out";
}