using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Application.Contracts;
using Guild.Application.Features.Guilds.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Guilds.UpdateGuild;

internal sealed class UpdateGuildHandler(
	IGuildRepository repository,
	IEventBus eventBus,
	IClock clock,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
	: ICommandHandler<UpdateGuildCommand, Result<GuildDto>>
{
	public async Task<Result<GuildDto>> HandleAsync(
		UpdateGuildCommand command,
		CancellationToken cancellationToken = default)
	{
		var auth = await AuthorizationContext.LoadAsync(
			repository, currentUser, command.GuildId, Permission.ManageGuild, cancellationToken);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;

		var updateResult = guild.UpdateSettings(
			name: command.Name,
			description: command.Description,
			iconUrl: command.IconUrl,
			bannerUrl: command.BannerUrl,
			now: clock.UtcNow);

		if (updateResult.IsFailure)
			return updateResult.Error;

		await eventBus.PublishAsync(
			new GuildUpdated(guild.Id, guild.Name, guild.IconUrl),
			cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return GuildDto.FromEntity(guild);
	}
}
