using Guild.Application.Abstractions.Messaging;

namespace Guild.Application.Features.Invites.CleanupExpiredInvites;

/// <summary>
/// fire-and-forget maintenance command dispatched by the hosted invite-cleanup
/// worker on a fixed interval. the handler hard-deletes revoked invites and
/// invites past their expiry grace period. carries no payload: the handler owns
/// the cleanup policy (grace window, what counts as eligible)
/// </summary>
public sealed record CleanupExpiredInvitesCommand : ICommand;
