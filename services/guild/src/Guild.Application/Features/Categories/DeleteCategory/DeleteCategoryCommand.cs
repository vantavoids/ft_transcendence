using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.Categories.DeleteCategory;

public sealed record DeleteCategoryCommand(
	long GuildId,
	long CategoryId) : ICommand<Result>;
