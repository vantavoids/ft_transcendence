using Carter;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.HelloWorld;
using Chat.Domain.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Presentation.Endpoints;

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
