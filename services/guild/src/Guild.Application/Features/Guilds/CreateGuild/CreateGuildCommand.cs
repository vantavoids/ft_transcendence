using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Guilds.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Guilds.CreateGuild;

public sealed record CreateGuildCommand(
	string? Name,
	string? Description,
	string? IconUrl,
	string? BannerUrl) : ICommand<Result<GuildDto>>;
