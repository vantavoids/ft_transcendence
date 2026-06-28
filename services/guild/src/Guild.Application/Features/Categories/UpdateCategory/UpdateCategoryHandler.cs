using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Application.Features.Categories.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Categories.UpdateCategory;

internal sealed class UpdateCategoryHandler(
	IGuildRepository guilds,
	IChannelCategoryRepository categories,
	IClock clock,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
	: ICommandHandler<UpdateCategoryCommand, Result<CategoryResponse>>
{
	public async Task<Result<CategoryResponse>> HandleAsync(
		UpdateCategoryCommand command,
		CancellationToken cancellationToken = default)
	{
		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, command.GuildId, Permission.ManageChannels, cancellationToken);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;

		var category = await categories.GetByIdAsync(command.GuildId, command.CategoryId, cancellationToken);
		if (category is null)
			return GuildFailures.CategoryNotFound;

		var now = clock.UtcNow;

		if (command.Name is not null)
		{
			var renameResult = category.Rename(command.Name, now);
			if (renameResult.IsFailure)
				return renameResult.Error;
		}

		if (command.Position is int newPosition)
		{
			var repositionResult = category.Reposition(newPosition, now);
			if (repositionResult.IsFailure)
				return repositionResult.Error;
		}

		categories.Update(category);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return CategoryResponse.From(category);
	}
}
