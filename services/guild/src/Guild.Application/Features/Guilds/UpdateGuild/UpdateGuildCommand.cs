using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Guilds.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Guilds.UpdateGuild;

public sealed record UpdateGuildCommand(
	long GuildId,
	string? Name,
	string? Description,
	string? IconUrl,
	string? BannerUrl) : ICommand<Result<GuildDto>>;
