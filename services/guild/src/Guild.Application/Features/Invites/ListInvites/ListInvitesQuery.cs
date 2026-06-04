using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Invites.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Invites.ListInvites;

public sealed record ListInvitesQuery(long GuildId) : IQuery<Result<IReadOnlyList<InviteDto>>>;
