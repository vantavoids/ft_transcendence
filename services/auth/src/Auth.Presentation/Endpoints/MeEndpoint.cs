using Carter;
using Auth.Application.Abstractions.Messaging;
using Auth.Application.Abstractions.Security;
using Auth.Application.Features.GetMe;
using Auth.Domain.Results;

using GetMeHttpResults = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.Ok<Auth.Application.Features.GetMe.GetMeResponse>,
    Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult
>;

namespace Auth.Presentation.Endpoints;

public sealed class MeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/me", async (
            ICurrentUser currentUser,
            IQueryHandler<GetMeQuery, Result<GetMeResponse>> handler,
            HttpContext ctx
        ) => await GetMe(currentUser, handler, ctx))
        .RequireAuthorization();
    }

    private static async Task<GetMeHttpResults> GetMe(
        ICurrentUser currentUser,
        IQueryHandler<GetMeQuery, Result<GetMeResponse>> handler,
        HttpContext ctx)
    {
        var result = await handler.HandleAsync(new GetMeQuery(currentUser.Id), ctx.RequestAborted);

        return result.Match<GetMeHttpResults>(
            value => TypedResults.Ok(value),
            _     => TypedResults.Unauthorized()
        );
    }
}
