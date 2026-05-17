using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Categories.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Categories.UpdateCategory;

public sealed record UpdateCategoryCommand(
	long GuildId,
	long CategoryId,
	string? Name,
	int? Position) : ICommand<Result<CategoryResponse>>;
