namespace Billing.Application.Common;

public sealed class ServiceResult<T>
{
    public bool Succeeded { get; init; }
    public int StatusCode { get; init; } = 200;
    public string? Error { get; init; }
    public T? Value { get; init; }

    public static ServiceResult<T> Ok(T value) => new() { Succeeded = true, Value = value };

    public static ServiceResult<T> Fail(int statusCode, string error) =>
        new() { Succeeded = false, StatusCode = statusCode, Error = error };
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
