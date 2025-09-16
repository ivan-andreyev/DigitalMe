namespace DigitalMe.Common;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// Follows the Result<T> pattern for improved error handling consistency.
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
public class Result<T>
{
    private Result(T? value, bool isSuccess, string error)
    {
        Value = value;
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// The value when the operation succeeded
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Error message when the operation failed
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Creates a successful result with the given value
    /// </summary>
    public static Result<T> Success(T value) => new(value, true, string.Empty);

    /// <summary>
    /// Creates a failed result with the given error message
    /// </summary>
    public static Result<T> Failure(string error) => new(default, false, error);

    /// <summary>
    /// Creates a failed result with exception details
    /// </summary>
    public static Result<T> Failure(Exception exception) => new(default, false, exception.Message);

    /// <summary>
    /// Implicit conversion from T to Result<T>
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Executes the appropriate action based on the result state
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error);
    }

    /// <summary>
    /// Executes the appropriate action based on the result state
    /// </summary>
    public void Match(Action<T> onSuccess, Action<string> onFailure)
    {
        if (IsSuccess)
            onSuccess(Value!);
        else
            onFailure(Error);
    }
}

/// <summary>
/// Non-generic Result for operations that don't return a value
/// </summary>
public class Result
{
    private Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Error message when the operation failed
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static Result Success() => new(true, string.Empty);

    /// <summary>
    /// Creates a failed result with the given error message
    /// </summary>
    public static Result Failure(string error) => new(false, error);

    /// <summary>
    /// Creates a failed result with exception details
    /// </summary>
    public static Result Failure(Exception exception) => new(false, exception.Message);

    /// <summary>
    /// Executes the appropriate action based on the result state
    /// </summary>
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Error);
    }

    /// <summary>
    /// Executes the appropriate action based on the result state
    /// </summary>
    public void Match(Action onSuccess, Action<string> onFailure)
    {
        if (IsSuccess)
            onSuccess();
        else
            onFailure(Error);
    }
}