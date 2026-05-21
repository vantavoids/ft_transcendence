using Auth.Presentation.Contracts;

namespace Auth.Presentation.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (BadHttpRequestException ex)
        {
            // model binding failures (missing/invalid query params, malformed body)
            // surface as 400 with the binder's own message, not 500
            logger.LogWarning(ex, "Bad request on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);

            // try retrieve error message from oauth provider (passed as query param)
            // fallback to excpetion message
            var queryErr = ctx.Request.Query["error"].ToString();
            var errResp = new ErrorResponse((!string.IsNullOrWhiteSpace(queryErr)) ? queryErr : ex.Message);

            ctx.Response.StatusCode = ex.StatusCode;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(errResp);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);

            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new ErrorResponse("An unexpected error occurred."));
        }
    }
}
