using Chat.Application.Abstractions.Messaging;
using Chat.Domain.Results;

namespace Chat.Application.Features.HelloWorld;

internal sealed class HelloWorldQueryHandler : IQueryHandler<HelloWorldQuery, Result<string>>
{
	public async Task<Result<string>> HandleAsync(HelloWorldQuery query, CancellationToken cancellationToken = default) =>
		"Hello, World!";
}
