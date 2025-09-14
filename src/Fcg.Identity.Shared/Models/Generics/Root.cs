using System.Collections.Generic;

namespace Fcg.Identity.Shared.Models.Generics;

public class Root<T>
    where T : class
{
    public int StatusCode { get; set; }

    public bool Success { get; set; }

    public T? Data { get; set; }

    public IEnumerable<string>? Errors { get; set; } = [];
}