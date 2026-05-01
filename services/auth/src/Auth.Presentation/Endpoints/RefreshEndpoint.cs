using Carter;
using Microsoft.Extensions.Options;
using Auth.Application.Abstractions;
using Auth.Application.Abstractions.Messaging;
using Auth.Application.Features.Refresh;
using Auth.Domain.Results;
using Auth.Infrastructure.Options;
using Auth.Presentation.Contracts.Refresh;

using RefreshEndpointHttpResults = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.Ok<
        Auth.Presentation.Contracts.Refresh.RefreshResponse
    >,
    Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult
>;

namespace Auth.Presentation.Endpoints;

public sealed class RefreshEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/refresh", async (
            HttpContext ctx,
            ICommandHandler<RefreshCommand, Result<RefreshResult>> handler,
            IClock clock,
            IOptions<RefreshTokenOptions> options
        ) => await Refresh(ctx, handler, clock, options.Value));
    }

    private static async Task<RefreshEndpointHttpResults> Refresh(
        HttpContext ctx,
        ICommandHandler<RefreshCommand, Result<RefreshResult>> handler,
        IClock clock,
        RefreshTokenOptions options)
    {
        var rawToken = ctx.Request.Cookies[options.CookieName];
        if (string.IsNullOrEmpty(rawToken))
            return TypedResults.Unauthorized();

        var command = new RefreshCommand(rawToken);
        var result = await handler.HandleAsync(command, ctx.RequestAborted);

        return result.Match<RefreshEndpointHttpResults>(
            r =>
            {
                ctx.Response.Cookies.Append(
                    options.CookieName,
                    r.RefreshToken,
                    new CookieOptions
                    {
                        HttpOnly = options.HttpOnly,
                        Secure = options.Secure,
                        SameSite = SameSiteMode.Strict,
                        Expires = clock.UtcNow.AddDays(options.TtlDays)
                    }
                );
                return TypedResults.Ok(new RefreshResponse(r.AccessToken));
            },
            _ => TypedResults.Unauthorized()
        );
    }
}
