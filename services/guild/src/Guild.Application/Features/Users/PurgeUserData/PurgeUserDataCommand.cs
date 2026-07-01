using Guild.Application.Abstractions.Messaging;

namespace Guild.Application.Features.Users.PurgeUserData;

/// <summary>
/// GDPR erasure cascade for a deleted account, dispatched by the
/// <c>user.deleted</c> consumer. removes rows referencing <see cref="UserId"/> as
/// a subject or sole artifact, and scrubs <c>banned_by</c> on bans they issued.
/// Auth's 409 ownership gate guarantees the user owns no guilds.
/// </summary>
public sealed record PurgeUserDataCommand(long UserId) : ICommand;
