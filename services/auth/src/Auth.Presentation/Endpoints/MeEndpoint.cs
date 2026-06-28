using Carter;
using Auth.Application.Abstractions.Messaging;
using Auth.Application.Abstractions.Security;
using Auth.Application.Features.DeleteMe;
using Auth.Application.Features.GetMe;
using Auth.Application.Features.UpdateMe;
using Auth.Domain.Results;
using Auth.Presentation.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Auth.Presentation.Endpoints;

public sealed class MeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder endpoints)
    {
        var me = endpoints.MapGroup("/me").RequireAuthorization();

        me.MapGet("",    GetMe);
        me.MapPatch("",  PatchMe);
        me.MapDelete("", DeleteMe);
    }

    private static async Task<Results<
        Ok<GetMeResponse>,
        UnauthorizedHttpResult>>
    GetMe(
        ICurrentUser currentUser,
        IQueryHandler<GetMeQuery, Result<GetMeResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetMeQuery(currentUser.Id), cancellationToken);

        return result.Succeeded
            ? TypedResults.Ok(result.Value)
            : TypedResults.Unauthorized();
    }

    private static async Task<Results<
        Ok<GetMeResponse>,
        UnauthorizedHttpResult,
        ForbidHttpResult,
        BadRequest<ErrorResponse>,
        JsonHttpResult<ErrorResponse>>>
    PatchMe(
        UpdateMeRequest body,
        ICurrentUser currentUser,
        ICommandHandler<UpdateMeCommand, Result<GetMeResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateMeCommand(
            currentUser.Id,
            body.Email,
            body.CurrentPassword,
            body.NewPassword);

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.Succeeded
            ? TypedResults.Ok(result.Value)
            : MapPatchError(result.Error);
    }

    private static async Task<Results<
        NoContent,
        UnauthorizedHttpResult,
        JsonHttpResult<ErrorResponse>>>
    DeleteMe(
        ICurrentUser currentUser,
        ICommandHandler<DeleteMeCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteMeCommand(currentUser.Id), cancellationToken);

        return result.Succeeded
            ? TypedResults.NoContent()
            : MapDeleteError(result.Error);
    }

    private static Results<Ok<GetMeResponse>, UnauthorizedHttpResult, ForbidHttpResult, BadRequest<ErrorResponse>, JsonHttpResult<ErrorResponse>>
        MapPatchError(Failure failure) => failure.Code switch
        {
            "Auth.InvalidAccessToken"     => TypedResults.Unauthorized(),
            "Auth.InvalidCredentials"     => TypedResults.Unauthorized(),
            "Auth.OAuthCantPatchEmail"    => TypedResults.Forbid(),
            "Auth.EmailAlreadyRegistered" => TypedResults.Json(new ErrorResponse(failure.Message), statusCode: StatusCodes.Status409Conflict),
            // "Auth.AtLeastOneFieldToPatch"
            _                             => TypedResults.BadRequest(new ErrorResponse(failure.Message)),
        };

    private static Results<NoContent, UnauthorizedHttpResult, JsonHttpResult<ErrorResponse>>
        MapDeleteError(Failure failure) => failure.Code switch
        {
            "Auth.InvalidAccessToken"  => TypedResults.Unauthorized(),
            "Auth.OwnedGuildsConflict" => TypedResults.Json(new ErrorResponse(failure.Message), statusCode: StatusCodes.Status409Conflict),
            _                          => TypedResults.Json(new ErrorResponse(failure.Message), statusCode: StatusCodes.Status500InternalServerError),
        };

    private sealed record UpdateMeRequest(
        string? Email,
        string? CurrentPassword,
        string? NewPassword);
}
