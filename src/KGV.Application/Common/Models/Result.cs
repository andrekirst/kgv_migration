namespace KGV.Application.Common.Models;

/// <summary>
/// Represents the result of an operation
/// </summary>
public class Result
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? Error { get; private set; }

    /// <summary>
    /// Additional error details
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors { get; private set; }

    protected Result(bool isSuccess, string? error = null, Dictionary<string, string[]>? validationErrors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        ValidationErrors = validationErrors;
    }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static Result Success() => new(true);

    /// <summary>
    /// Creates a failed result
    /// </summary>
    /// <param name="error">Error message</param>
    public static Result Failure(string error) => new(false, error);

    /// <summary>
    /// Creates a failed result with validation errors
    /// </summary>
    /// <param name="validationErrors">Validation errors</param>
    public static Result ValidationFailure(Dictionary<string, string[]> validationErrors) 
        => new(false, "Validation failed", validationErrors);

    /// <summary>
    /// Creates a failed result with a single validation error
    /// </summary>
    /// <param name="field">Field name</param>
    /// <param name="errors">Error messages</param>
    public static Result ValidationFailure(string field, params string[] errors)
        => new(false, "Validation failed", new Dictionary<string, string[]> { [field] = errors });
}

/// <summary>
/// Represents the result of an operation with a return value
/// </summary>
/// <typeparam name="T">Type of the return value</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// The return value if the operation was successful
    /// </summary>
    public T? Value { get; private set; }

    private Result(bool isSuccess, T? value = default, string? error = null, Dictionary<string, string[]>? validationErrors = null)
        : base(isSuccess, error, validationErrors)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a successful result with a value
    /// </summary>
    /// <param name="value">The return value</param>
    public static Result<T> Success(T value) => new(true, value);

    /// <summary>
    /// Creates a failed result
    /// </summary>
    /// <param name="error">Error message</param>
    public static new Result<T> Failure(string error) => new(false, default, error);

    /// <summary>
    /// Creates a failed result with validation errors
    /// </summary>
    /// <param name="validationErrors">Validation errors</param>
    public static new Result<T> ValidationFailure(Dictionary<string, string[]> validationErrors) 
        => new(false, default, "Validation failed", validationErrors);

    /// <summary>
    /// Creates a failed result with a single validation error
    /// </summary>
    /// <param name="field">Field name</param>
    /// <param name="errors">Error messages</param>
    public static new Result<T> ValidationFailure(string field, params string[] errors)
        => new(false, default, "Validation failed", new Dictionary<string, string[]> { [field] = errors });

    /// <summary>
    /// Implicitly converts from T to Result&lt;T&gt;
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Maps the value to a different type if the result is successful
    /// </summary>
    /// <typeparam name="TResult">Target type</typeparam>
    /// <param name="mapper">Mapping function</param>
    public Result<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        if (IsFailure)
            return Result<TResult>.Failure(Error ?? "Operation failed");

        try
        {
            var mappedValue = mapper(Value!);
            return Result<TResult>.Success(mappedValue);
        }
        catch (Exception ex)
        {
            return Result<TResult>.Failure($"Mapping failed: {ex.Message}");
        }
    }
}