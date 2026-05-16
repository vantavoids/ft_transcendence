using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Guilds.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Guilds.GetGuild;

public sealed record GetGuildQuery(long GuildId) : IQuery<Result<GuildDetailsDto>>;
