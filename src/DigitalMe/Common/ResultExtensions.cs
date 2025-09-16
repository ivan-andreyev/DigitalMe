namespace DigitalMe.Common;

/// <summary>
/// Extension methods for Result<T> pattern to support async operations and functional composition
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Safely executes an async operation and wraps the result in Result<T>
    /// </summary>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> operation, string? errorContext = null)
    {
        try
        {
            var result = await operation();
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            var errorMessage = errorContext != null
                ? $"{errorContext}: {ex.Message}"
                : ex.Message;
            return Result<T>.Failure(errorMessage);
        }
    }

    /// <summary>
    /// Safely executes a sync operation and wraps the result in Result<T>
    /// </summary>
    public static Result<T> Try<T>(Func<T> operation, string? errorContext = null)
    {
        try
        {
            var result = operation();
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            var errorMessage = errorContext != null
                ? $"{errorContext}: {ex.Message}"
                : ex.Message;
            return Result<T>.Failure(errorMessage);
        }
    }

    /// <summary>
    /// Safely executes an async action and wraps the result in Result
    /// </summary>
    public static async Task<Result> TryAsync(Func<Task> operation, string? errorContext = null)
    {
        try
        {
            await operation();
            return Result.Success();
        }
        catch (Exception ex)
        {
            var errorMessage = errorContext != null
                ? $"{errorContext}: {ex.Message}"
                : ex.Message;
            return Result.Failure(errorMessage);
        }
    }

    /// <summary>
    /// Maps the success value to a new type
    /// </summary>
    public static Result<TOutput> Map<TInput, TOutput>(this Result<TInput> result, Func<TInput, TOutput> mapper)
    {
        return result.IsSuccess
            ? Result<TOutput>.Success(mapper(result.Value!))
            : Result<TOutput>.Failure(result.Error);
    }

    /// <summary>
    /// Chains operations together, only executing the next if the current succeeds
    /// </summary>
    public static Result<TOutput> Bind<TInput, TOutput>(this Result<TInput> result, Func<TInput, Result<TOutput>> binder)
    {
        return result.IsSuccess
            ? binder(result.Value!)
            : Result<TOutput>.Failure(result.Error);
    }

    /// <summary>
    /// Chains async operations together
    /// </summary>
    public static async Task<Result<TOutput>> BindAsync<TInput, TOutput>(this Result<TInput> result, Func<TInput, Task<Result<TOutput>>> binder)
    {
        return result.IsSuccess
            ? await binder(result.Value!)
            : Result<TOutput>.Failure(result.Error);
    }

    /// <summary>
    /// Chains async operations together for Task<Result<T>>
    /// </summary>
    public static async Task<Result<TOutput>> BindAsync<TInput, TOutput>(this Task<Result<TInput>> resultTask, Func<TInput, Task<Result<TOutput>>> binder)
    {
        var result = await resultTask;
        return result.IsSuccess
            ? await binder(result.Value!)
            : Result<TOutput>.Failure(result.Error);
    }

    /// <summary>
    /// Returns the value if success, otherwise returns the default value
    /// </summary>
    public static T ValueOrDefault<T>(this Result<T> result, T defaultValue = default!)
    {
        return result.IsSuccess ? result.Value! : defaultValue;
    }

    /// <summary>
    /// Throws an exception if the result is a failure, otherwise returns the value
    /// </summary>
    public static T ValueOrThrow<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? result.Value!
            : throw new InvalidOperationException($"Result failed: {result.Error}");
    }
}