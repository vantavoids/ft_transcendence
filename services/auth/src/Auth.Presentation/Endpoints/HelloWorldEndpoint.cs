using Carter;
using Auth.Application.Abstractions.Messaging;
using Auth.Application.Features.HelloWorld;
using Auth.Domain.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Auth.Presentation.Endpoints;

public sealed class HelloWorldEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/hello-world", HelloWorld);
    }

    private async Task<Results<Ok<string>, BadRequest<string>>> HelloWorld(
        IQueryHandler<HelloWorldQuery, Result<string>> handler
    )
    {
        var result = await handler.HandleAsync(new HelloWorldQuery());

        return result switch
        {
            { Succeeded: true, Value: var value } => TypedResults.Ok(value),
            _ => TypedResults.BadRequest("Something went wrong (somehow)")
        };
    }
}