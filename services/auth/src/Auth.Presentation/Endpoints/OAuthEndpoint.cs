using System.Collections.Frozen;
using Auth.Application.Abstractions;
using Auth.Application.Abstractions.Messaging;
using Auth.Application.Features.OAuth;
using Auth.Domain.AuthUser;
using Auth.Domain.Results;
using Auth.Infrastructure.Options;
using Auth.Infrastructure.Options.OAuth;
using Carter;
using Microsoft.Extensions.Options;
using Auth.Presentation.Contracts;

using InitOAuthHttpResults = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.RedirectHttpResult,
    Microsoft.AspNetCore.Http.HttpResults.BadRequest<Auth.Presentation.Contracts.ErrorResponse>
>;
using OAuthCallbackHttpResults = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.RedirectHttpResult,
    Microsoft.AspNetCore.Http.HttpResults.BadRequest<Auth.Presentation.Contracts.ErrorResponse>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<Auth.Presentation.Contracts.ErrorResponse>
>;

namespace Auth.Presentation.Endpoints;

public sealed class OAuthEndpoint : ICarterModule
{
    private static readonly FrozenDictionary<string, OAuthProvider> ProviderMap =
        new Dictionary<string, OAuthProvider>
        {
            ["fortytwo"] = OAuthProvider.FortyTwo,
            ["google"]   = OAuthProvider.Google,
            ["github"]   = OAuthProvider.Github,
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/oauth/{provider}", async (
            string provider,
            ICommandHandler<OAuthLoginCommand, Result<OAuthLoginResult>> handler,
            IClock clock,
            IOptions<OAuthStateCookieOptions> stateOptions,
            HttpResponse resp
        ) => await InitOAuth(provider, handler, clock, stateOptions.Value, resp));

        app.MapGet("/oauth/{provider}/callback", async (
            string provider,
            string code,
            string state,
            ICommandHandler<OAuthCallbackCommand, Result<OAuthCallbackResult>> handler,
            IClock clock,
            IOptions<OAuthStateCookieOptions> stateOptions,
            IOptions<RefreshTokenOptions> refreshOptions,
            IOptions<AppOptions> appOptions,
            HttpContext ctx
        ) => await OAuthCallback(provider, code, state, handler, clock, stateOptions.Value, refreshOptions.Value, appOptions.Value, ctx));
    }

    private static async Task<InitOAuthHttpResults> InitOAuth(
        string provider,
        ICommandHandler<OAuthLoginCommand, Result<OAuthLoginResult>> handler,
        IClock clock,
        OAuthStateCookieOptions stateOpts,
        HttpResponse resp)
    {
        if (!ProviderMap.TryGetValue(provider, out var oauthProvider))
            return TypedResults.BadRequest(new ErrorResponse(AuthFailures.InvalidOAuthProvider.Message));

        var result = await handler.HandleAsync(new OAuthLoginCommand(oauthProvider));

        return result.Match<InitOAuthHttpResults>(
            r =>
            {
                resp.Cookies.Append(stateOpts.CookieName, r.State, new CookieOptions
                {
                    HttpOnly = stateOpts.HttpOnly,
                    Secure   = stateOpts.Secure,
                    SameSite = SameSiteMode.Lax,
                    Expires  = clock.UtcNow.AddMinutes(stateOpts.TtlMinutes),
                });
                return TypedResults.Redirect(r.RedirectUri.ToString());
            },
            failure => TypedResults.BadRequest(new ErrorResponse(failure.Message))
        );
    }

    private static async Task<OAuthCallbackHttpResults> OAuthCallback(
        string provider,
        string code,
        string state,
        ICommandHandler<OAuthCallbackCommand, Result<OAuthCallbackResult>> handler,
        IClock clock,
        OAuthStateCookieOptions stateOpts,
        RefreshTokenOptions refreshOpts,
        AppOptions appOpts,
        HttpContext ctx)
    {
        if (!ProviderMap.TryGetValue(provider, out var oauthProvider))
            return TypedResults.BadRequest(new ErrorResponse(AuthFailures.InvalidOAuthProvider.Message));

        var storedState = ctx.Request.Cookies[stateOpts.CookieName];
        if (string.IsNullOrEmpty(storedState) || storedState != state)
            return TypedResults.BadRequest(new ErrorResponse(AuthFailures.InvalidOAuthState.Message));

        ctx.Response.Cookies.Delete(stateOpts.CookieName);

        var result = await handler.HandleAsync(new OAuthCallbackCommand(oauthProvider, code, state));

        if (result.IsFailure)
        {
            var error = new ErrorResponse(result.Error.Message);
            return result.Error == AuthFailures.OAuthUpstreamError
                ? TypedResults.Json(error, statusCode: StatusCodes.Status502BadGateway)
                : TypedResults.BadRequest(error);
        }

        var r = result.Value;
        ctx.Response.Cookies.Append(refreshOpts.CookieName, r.RefreshToken, new CookieOptions
        {
            HttpOnly = refreshOpts.HttpOnly,
            Secure   = refreshOpts.Secure,
            SameSite = SameSiteMode.Strict,
            Expires  = clock.UtcNow.AddDays(refreshOpts.TtlDays),
        });

        var baseUrl = appOpts.BaseUrl.TrimEnd('/');
        var destination = r.IsNewUser
            ? $"{baseUrl}/?access_token={r.AccessToken}&new_user=true"
            : $"{baseUrl}/?access_token={r.AccessToken}";

        return TypedResults.Redirect(destination);
    }
}
