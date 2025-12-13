#nullable enable

using System.Net;

namespace explainpowershell.frontend.Clients;

public sealed class ApiCallResult<T>
{
    private ApiCallResult() { }

    public bool IsSuccess { get; private init; }

    public HttpStatusCode StatusCode { get; private init; }

    public T? Value { get; private init; }

    public string? ErrorMessage { get; private init; }

    public static ApiCallResult<T> Success(T value, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new()
        {
            IsSuccess = true,
            StatusCode = statusCode,
            Value = value,
            ErrorMessage = null
        };

    public static ApiCallResult<T> Failure(string? errorMessage, HttpStatusCode statusCode)
        => new()
        {
            IsSuccess = false,
            StatusCode = statusCode,
            Value = default,
            ErrorMessage = errorMessage
        };
}
