using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Guilds.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Membership.JoinByInviteCode;

/// <summary>
/// shared command behind <c>POST /guilds/{id}/join</c> and <c>POST /invites/{code}/join</c>.
/// when <see cref="ExpectedGuildId"/> is set the handler rejects the invite if it
/// belongs to a different guild (so the URL path stays authoritative).
/// </summary>
public sealed record JoinByInviteCodeCommand(string? Code, long? ExpectedGuildId)
	: ICommand<Result<GuildDto>>;
