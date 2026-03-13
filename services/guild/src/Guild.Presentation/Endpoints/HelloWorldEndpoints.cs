using Carter;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.HelloWorld;
using Guild.Domain.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Guild.Presentation.Endpoints;

public sealed class HelloWorldEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/hello-world", HelloWorld);
    }

    private static async Task<Results<Ok<string>, BadRequest<string>>> HelloWorld(
        IQueryHandler<HelloWorldQuery, Result<string>> handler)
    {
        var result = await handler.HandleAsync(new HelloWorldQuery());

        return result switch
        {
            { Succeeded: true, Value: var value } => TypedResults.Ok(value),
            _ => TypedResults.BadRequest("Something went wrong (somehow)")
        };
    }
}