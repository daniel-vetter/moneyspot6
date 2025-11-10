namespace MoneySpot6.WebApp.Infrastructure;

public class Result<TSuccess, TError>
{
    public TSuccess? Success { get; }
    public TError? Error { get; }

    private Result(TSuccess? success, TError? error)
    {
        Success = success;
        Error = error;
    }

    public static Result<TSuccess, TError> Ok(TSuccess success) => new Result<TSuccess, TError>(success, default);
    public static Result<TSuccess, TError> Fail(TError error) => new Result<TSuccess, TError>(default, error);

    public TResult Match<TResult>(Func<TSuccess, TResult> onSuccess, Func<TError, TResult> onError)
    {
        if (Success != null)
            return onSuccess(Success);
        if (Error != null)
            return onError(Error);

        throw new InvalidDataException();
    }
}

public class Result<TError>
{
    public TError? Error { get; }

    private Result(TError? error)
    {
        Error = error;
    }

    public static Result<TError> Ok() => new Result<TError>(default);
    public static Result<TError> Fail(TError error) => new Result<TError>(error);

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<TError, TResult> onError)
    {
        if (Error == null)
            return onSuccess();
        if (Error != null)
            return onError(Error);

        throw new InvalidDataException();
    }
}