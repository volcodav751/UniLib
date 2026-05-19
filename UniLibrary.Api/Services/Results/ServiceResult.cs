namespace UniLibrary.Api.Services.Results;

public enum ServiceResultStatus
{
    Ok,
    Created,
    NoContent,
    BadRequest,
    Unauthorized,
    Forbidden,
    NotFound
}

public class ServiceResult
{
    public ServiceResultStatus Status { get; init; }
    public string? Error { get; init; }

    public bool IsSuccess => Status is ServiceResultStatus.Ok
        or ServiceResultStatus.Created
        or ServiceResultStatus.NoContent;

    public static ServiceResult Ok()
    {
        return new ServiceResult { Status = ServiceResultStatus.Ok };
    }

    public static ServiceResult NoContent()
    {
        return new ServiceResult { Status = ServiceResultStatus.NoContent };
    }

    public static ServiceResult BadRequest(string error)
    {
        return new ServiceResult { Status = ServiceResultStatus.BadRequest, Error = error };
    }

    public static ServiceResult Unauthorized(string? error = null)
    {
        return new ServiceResult { Status = ServiceResultStatus.Unauthorized, Error = error };
    }

    public static ServiceResult Forbidden(string? error = null)
    {
        return new ServiceResult { Status = ServiceResultStatus.Forbidden, Error = error };
    }

    public static ServiceResult NotFound(string? error = null)
    {
        return new ServiceResult { Status = ServiceResultStatus.NotFound, Error = error };
    }
}

public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; init; }

    public static ServiceResult<T> Ok(T value)
    {
        return new ServiceResult<T> { Status = ServiceResultStatus.Ok, Value = value };
    }

    public static ServiceResult<T> Created(T value)
    {
        return new ServiceResult<T> { Status = ServiceResultStatus.Created, Value = value };
    }

    public new static ServiceResult<T> BadRequest(string error)
    {
        return new ServiceResult<T> { Status = ServiceResultStatus.BadRequest, Error = error };
    }

    public new static ServiceResult<T> Unauthorized(string? error = null)
    {
        return new ServiceResult<T> { Status = ServiceResultStatus.Unauthorized, Error = error };
    }

    public new static ServiceResult<T> Forbidden(string? error = null)
    {
        return new ServiceResult<T> { Status = ServiceResultStatus.Forbidden, Error = error };
    }

    public new static ServiceResult<T> NotFound(string? error = null)
    {
        return new ServiceResult<T> { Status = ServiceResultStatus.NotFound, Error = error };
    }

    public static ServiceResult<T> From(ServiceResult result)
    {
        return new ServiceResult<T> { Status = result.Status, Error = result.Error };
    }

    public static ServiceResult<T> From<TOther>(ServiceResult<TOther> result)
    {
        return new ServiceResult<T> { Status = result.Status, Error = result.Error };
    }
}
