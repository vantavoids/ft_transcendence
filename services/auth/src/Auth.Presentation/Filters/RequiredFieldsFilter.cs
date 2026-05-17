using Auth.Presentation.Contracts;

namespace Auth.Presentation.Filters;

#nullable enable

public sealed class RequiredFieldsFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next)
    {
        var body = ctx.Arguments.OfType<T>().FirstOrDefault();
        if (body is null)
            return TypedResults.BadRequest(new ErrorResponse("Invalid request body."));

        var emptyProp = typeof(T).GetProperties()
            .Where(p => p.PropertyType == typeof(string))
            .FirstOrDefault(p => string.IsNullOrWhiteSpace(p.GetValue(body) as string));

        if (emptyProp is not null)
            return TypedResults.BadRequest(new ErrorResponse($"{emptyProp.Name} is required."));

        return await next(ctx);
    }
}
