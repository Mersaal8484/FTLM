namespace FuelTrack.Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public int StatusCode { get; }

    private Result(T value, int statusCode = 200)
    {
        IsSuccess = true;
        Value = value;
        Error = null;
        StatusCode = statusCode;
    }

    private Result(string error, int statusCode = 400)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
        StatusCode = statusCode;
    }

    public static Result<T> Success(T value, int statusCode = 200)
        => new(value, statusCode);

    public static Result<T> Failure(string error, int statusCode = 400)
        => new(error, statusCode);

    public static Result<T> NotFound(string error = "Resource not found")
        => new(error, 404);

    public static Result<T> Unauthorized(string error = "Unauthorized")
        => new(error, 401);

    public static Result<T> Forbidden(string error = "Forbidden")
        => new(error, 403);

    public static Result<T> Conflict(string error = "Conflict")
        => new(error, 409);
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public int StatusCode { get; }

    private Result(int statusCode = 200)
    {
        IsSuccess = true;
        Error = null;
        StatusCode = statusCode;
    }

    private Result(string error, int statusCode = 400)
    {
        IsSuccess = false;
        Error = error;
        StatusCode = statusCode;
    }

    public static Result Success(int statusCode = 200)
        => new(statusCode);

    public static Result Failure(string error, int statusCode = 400)
        => new(error, statusCode);

    public static Result NotFound(string error = "Resource not found")
        => new(error, 404);
}
