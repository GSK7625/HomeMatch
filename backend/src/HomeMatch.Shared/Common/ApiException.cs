namespace HomeMatch.Shared.Common;

public sealed class ApiException(int statusCode, string code, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;

    public static ApiException BadRequest(string code, string message) => new(400, code, message);
    public static ApiException Forbidden(string code, string message) => new(403, code, message);
    public static ApiException NotFound(string code, string message) => new(404, code, message);
    public static ApiException Conflict(string code, string message) => new(409, code, message);
}
