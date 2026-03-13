using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.HelloWorld;

internal sealed class HelloWorldQueryHandler : IQueryHandler<HelloWorldQuery, Result<string>>
{
    public async Task<Result<string>> HandleAsync(HelloWorldQuery query, CancellationToken cancellationToken = default) =>
        "Hello, World!";
}