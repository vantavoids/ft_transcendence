namespace Auth.Domain.Results;

public sealed record Failure(string Code, string Message)
{
    internal static readonly Failure None = new(string.Empty, string.Empty);
    public static readonly Failure NullValue = new("Failure.NullValue", "Value cannot be null.");

    public static implicit operator Result(Failure error) => Result.Fail(error);
}