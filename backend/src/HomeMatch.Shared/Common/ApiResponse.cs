namespace HomeMatch.Shared.Common;

public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public Error? Error { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Success(T data) => new()
    {
        IsSuccess = true,
        Data = data
    };

    public static ApiResponse<T> Failure(Error error) => new()
    {
        IsSuccess = false,
        Error = error
    };
}

public static class ApiResponse
{
    public static ApiResponse<T> Success<T>(T data) => ApiResponse<T>.Success(data);
    public static ApiResponse<T> Failure<T>(Error error) => ApiResponse<T>.Failure(error);
}
