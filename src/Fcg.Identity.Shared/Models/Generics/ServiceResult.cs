using System.Collections.Generic;
using System.Net;

namespace Fcg.Identity.Shared.Models.Generics;

public class ServiceResult<T>
{
    public bool Success { get; }
    public T? Data { get; }
    public List<string> Errors { get; } = [];
    public HttpStatusCode StatusCode { get; }

    private ServiceResult(
        bool success,
        T? data,
        HttpStatusCode statusCode,
        IEnumerable<string>? errors = null
    )
    {
        Success = success;
        Data = data;
        StatusCode = statusCode;
        if (errors != null)
            Errors.AddRange(errors);
    }

    public static ServiceResult<T> SuccessResult(
        T data,
        HttpStatusCode statusCode = HttpStatusCode.OK
    ) => new(true, data, statusCode);

    public static ServiceResult<T> FailureResult(
        HttpStatusCode statusCode,
        params string[] errors
    ) => new(false, default, statusCode, errors);

    public static ServiceResult<T> NotFoundResult(string message) =>
        new(false, default, HttpStatusCode.NotFound, new[] { message });
}
