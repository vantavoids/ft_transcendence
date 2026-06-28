using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Application.Features.Categories.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Categories.CreateCategory;

internal sealed class CreateCategoryHandler(
	IGuildRepository guilds,
	IChannelCategoryRepository categories,
	IIdGenerator ids,
	IClock clock,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
	: ICommandHandler<CreateCategoryCommand, Result<CategoryResponse>>
{
	public async Task<Result<CategoryResponse>> HandleAsync(
		CreateCategoryCommand command,
		CancellationToken cancellationToken = default)
	{
		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, command.GuildId, Permission.ManageChannels, cancellationToken);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;

		int position;
		if (command.Position is int requested)
		{
			position = requested;
		}
		else
		{
			var max = await categories.GetMaxPositionAsync(command.GuildId, cancellationToken);
			position = (max ?? -1) + 1;
		}

		var categoryResult = ChannelCategory.Create(
			id: ids.NextId(),
			guildId: command.GuildId,
			name: command.Name,
			position: position,
			now: clock.UtcNow);

		if (categoryResult.IsFailure)
			return categoryResult.Error;

		await categories.AddAsync(categoryResult.Value, cancellationToken);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return CategoryResponse.From(categoryResult.Value);
	}
}
