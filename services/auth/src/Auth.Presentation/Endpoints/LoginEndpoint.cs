using Carter;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http.HttpResults;
using Auth.Application.Abstractions.Messaging;
using Auth.Application.Abstractions;
using Auth.Application.Features.Login;
using Auth.Domain.Results;
using Auth.Infrastructure.Options;
using Auth.Presentation.Contracts.Responses;

// TODO: Confirm (user not found, user soft deleted, no password) => 401
using LoginEndpointHttpResults = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.Ok<
        Auth.Presentation.Contracts.Responses.LoginResponse
    >,
    Microsoft.AspNetCore.Http.HttpResults.BadRequest<
        Auth.Presentation.Contracts.Responses.ErrorResponse
    >,
    Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult
>;

namespace Auth.Presentation.Endpoints;

public sealed class LoginEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/login", async (
            LoginCommand req,
            ICommandHandler<LoginCommand, Result<LoginResult>> handler,
            IClock clock,
            IOptions<RefreshTokenOptions> options,
            HttpResponse resp
        ) => await Login(req, handler, clock, options.Value, resp));
    }

    private static async Task<LoginEndpointHttpResults> Login(
        LoginCommand req,
        ICommandHandler<LoginCommand, Result<LoginResult>> handler,
        IClock clock,
        RefreshTokenOptions options,
        HttpResponse resp)
    {
        var result = await handler.HandleAsync(req);

        return result.Match<LoginEndpointHttpResults>(
            r =>
            {
                resp.Cookies.Append(
                    options.CookieName,
                    r.RefreshToken,
                    new CookieOptions
                    {
                        HttpOnly = options.HttpOnly,
                        Secure = options.Secure,
                        SameSite = SameSiteMode.Strict,
                        Expires = clock.UtcNow.Add(TimeSpan.FromDays(options.TtlDays))
                    }
                );
                return TypedResults.Ok(new LoginResponse(r.UserId, r.AccessToken));
            },
            failure => failure.Code switch
            {
                _ when failure.Code == AuthFailures.InvalidCredentials.Code => TypedResults.Unauthorized(),
                _ => TypedResults.BadRequest(new ErrorResponse(failure.Message))
            }
        );
    }
}
