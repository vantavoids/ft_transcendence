using Auth.Application.Abstractions.Messaging;
using Auth.Domain.Results;

namespace Auth.Application.Features.HelloWorld;

internal sealed class HelloWorldQueryHandler: IQueryHandler<HelloWorldQuery, Result<string>>
{
    public async Task<Result<string>> HandleAsync(HelloWorldQuery query, CancellationToken cancellationToken = default) =>
        "Hello world !";
}
