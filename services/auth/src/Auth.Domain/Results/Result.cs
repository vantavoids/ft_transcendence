namespace Auth.Domain.Results;

public class Result
{
    private protected Result(bool succeeded, Failure error)
    {
        Succeeded = succeeded;
        Error = error;
    }

    public bool Succeeded { get; }
    public Failure Error { get; }
    public bool IsFailure => !Succeeded;

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Failure, TResult> onFailure) =>
        Succeeded ? onSuccess() : onFailure(Error);

    public static Result Ok() => new(true, Failure.None);
    public static Result Fail(Failure error) => new(false, error);
    public static Result<TValue> Ok<TValue>(TValue value) => new(true, value, Failure.None);
    public static Result<TValue> Fail<TValue>(Failure error) => new(false, default!, error);
}

public sealed class Result<TValue>(bool isSuccess, TValue value, Failure error) : Result(isSuccess, error)
{
    public TValue Value => Succeeded
        ? value
        : throw new InvalidOperationException(
            "There is no value when a failure occurs. Check that IsFailure is false before accessing Value.");

    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<Failure, TResult> onFailure) =>
        Succeeded ? onSuccess(Value) : onFailure(Error);

    public static implicit operator Result<TValue>(TValue value) => Ok(value);
    public static implicit operator Result<TValue>(Failure failure) => Fail<TValue>(failure);
}