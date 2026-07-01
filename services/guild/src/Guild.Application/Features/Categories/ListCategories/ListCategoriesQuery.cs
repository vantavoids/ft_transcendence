using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Categories.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Categories.ListCategories;

public sealed record ListCategoriesQuery(long GuildId) : IQuery<Result<CategoryListResponse>>;

/// <summary>
/// wrapped in a record so the generic <c>IQueryHandler&lt;,&gt;</c> constraint
/// (<c>TResponse : class</c>) is satisfied; serialised by Carter as a JSON
/// array via <see cref="Items"/>
/// </summary>
public sealed record CategoryListResponse(IReadOnlyList<CategoryResponse> Items);
