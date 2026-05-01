using Carter;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Auth.Application.Abstractions.Messaging;
using Auth.Application.Features.Register;
using Auth.Domain.Results;
using Auth.Infrastructure.Options;
using Auth.Application.Abstractions;
using Auth.Presentation.Contracts;
using Auth.Presentation.Contracts.Register;

using RegisterEndpointHttpResults = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.Created<Auth.Presentation.Contracts.Register.RegisterResponse>,
    Microsoft.AspNetCore.Http.HttpResults.BadRequest<Auth.Presentation.Contracts.ErrorResponse>,
    Microsoft.AspNetCore.Http.HttpResults.Conflict<Auth.Presentation.Contracts.ErrorResponse>
>;

public sealed class RegisterEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/register", async (
            RegisterRequest req,
            ICommandHandler<RegisterCommand, Result<RegisterResult>> handler,
            IClock clock,
            IOptions<RefreshTokenOptions> options,
            HttpResponse resp
        ) => await Register(req, handler, clock, options.Value, resp));
    }


    private async Task<RegisterEndpointHttpResults> Register(
        RegisterRequest req,
        ICommandHandler<RegisterCommand, Result<RegisterResult>> handler,
        IClock clock,
        RefreshTokenOptions options,
        HttpResponse resp
    )
    {
        var command = new RegisterCommand(req.Email, req.Password);
        var result = await handler.HandleAsync(command);

        return result.Match<RegisterEndpointHttpResults>(
            r => {
                resp.Cookies.Append(
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
                return TypedResults.Created($"/users/{r.UserId}", new RegisterResponse(r.UserId, r.AccessToken));
            },
            failure => {
                var errResp = new ErrorResponse(failure.Message);
                if (failure.Code == AuthFailures.EmailAlreadyRegistered.Code)
                    return TypedResults.Conflict(errResp);
                return TypedResults.BadRequest(errResp);
            }
        );
    }
}