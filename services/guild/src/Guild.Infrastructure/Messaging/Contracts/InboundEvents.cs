using MassTransit;

namespace Guild.Infrastructure.Messaging.Contracts;

// cross-service events Guild consumes but does not publish. the [MessageUrn]
// must match the publisher's URN or the message routes to *_skipped; it lives in
// Infrastructure to keep MassTransit out of the Application layer. exchange names
// are bound via SetEntityName in DependencyInjection.

// published by Auth on DELETE /auth/me (exchange user.deleted).
[MessageUrn("Auth.Application.Contracts:UserDeleted")]
public sealed record UserDeleted(long UserId);
