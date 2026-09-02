namespace LocalLive.Application.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Unauthorized,
    Forbidden,
    Conflict,
    TooManyRequests,
    Server
}

public sealed record Error(ErrorType Type, string Code, string Message, string? Field = null);

public sealed class Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public Error? Error { get; init; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(Error error) => new() { IsSuccess = false, Error = error };

    public static implicit operator Result<T>(T value) => Success(value);
}

public sealed class Result
{
    public bool IsSuccess { get; init; }
    public Error? Error { get; init; }

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(Error error) => new() { IsSuccess = false, Error = error };
}
