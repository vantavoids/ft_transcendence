using Carter;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Auth.Application.Abstractions.Messaging;
using Auth.Application.Features.Logout;
using Auth.Domain.Results;
using Auth.Infrastructure.Options;

using LogoutEndpointHttpResults = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.NoContent,
    Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult
>;

namespace Auth.Presentation.Endpoints;

public sealed class LogoutEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/logout", async (
            HttpContext ctx,
            ICommandHandler<LogoutCommand, Result> handler,
            IOptions<RefreshTokenOptions> options
        ) => await Logout(ctx, handler, options.Value))
        .RequireAuthorization();
    }

    private static async Task<LogoutEndpointHttpResults> Logout(
        HttpContext ctx,
        ICommandHandler<LogoutCommand, Result> handler,
        RefreshTokenOptions options)
    {
        var userIdStr = ctx.User.FindFirst("sub")?.Value;
        if (!long.TryParse(userIdStr, out var userId))
            return TypedResults.Unauthorized();

        var command = new LogoutCommand(userId);
        var result = await handler.HandleAsync(command, ctx.RequestAborted);

        return result.Match<LogoutEndpointHttpResults>(
            () =>
            {
                ctx.Response.Cookies.Delete(options.CookieName, new CookieOptions
                {
                    Secure   = options.Secure,
                    SameSite = SameSiteMode.Strict,
                });
                return TypedResults.NoContent();
            },
            _ => TypedResults.Unauthorized()
        );
    }
}
