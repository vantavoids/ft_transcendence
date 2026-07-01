using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Features.Categories.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Categories.ListCategories;

internal sealed class ListCategoriesHandler(
	IGuildRepository guilds,
	IChannelCategoryRepository categories,
	ICurrentUser currentUser)
	: IQueryHandler<ListCategoriesQuery, Result<CategoryListResponse>>
{
	public async Task<Result<CategoryListResponse>> HandleAsync(
		ListCategoriesQuery query,
		CancellationToken cancellationToken = default)
	{
		var guild = await guilds.GetByIdAsync(query.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		var isMember = await guilds.IsMemberAsync(query.GuildId, currentUser.Id, cancellationToken);
		if (!isMember)
			return GuildFailures.NotAMember;

		var entities = await categories.GetByGuildAsync(query.GuildId, cancellationToken);
		var dtos = entities.Select(CategoryResponse.From).ToList();
		return new CategoryListResponse(dtos);
	}
}
