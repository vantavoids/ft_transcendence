using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Categories.DeleteCategory;

internal sealed class DeleteCategoryHandler(
	IGuildRepository guilds,
	IChannelCategoryRepository categories,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
	: ICommandHandler<DeleteCategoryCommand, Result>
{
	public async Task<Result> HandleAsync(
		DeleteCategoryCommand command,
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

		categories.Remove(category);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
