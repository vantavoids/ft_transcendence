using Auth.Application.Abstractions.Events;

namespace Auth.Application.Contracts;

// own-source events published by Auth. namespace + type name form the default
// MassTransit URN ("Auth.Application.Contracts:UserDeleted") that consumers bind
// to; no [MessageUrn] attribute here keeps MassTransit out of Application (see
// DomainPurityTests). exchange names are set in Auth.Infrastructure.
public sealed record UserRegistered(long UserId, string Email) : IEvent
{
    public string EventType => "user.registered";
}

public sealed record UserLoggedOut(long UserId) : IEvent
{
    public string EventType => "user.logged_out";
}

public sealed record UserDeleted(long UserId) : IEvent
{
    public string EventType => "user.deleted";
}
