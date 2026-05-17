using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Categories.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Categories.CreateCategory;

public sealed record CreateCategoryCommand(
	long GuildId,
	string? Name,
	int? Position) : ICommand<Result<CategoryResponse>>;
