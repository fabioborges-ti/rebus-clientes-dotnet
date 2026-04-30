namespace Rebus.Clientes.Api.Models;

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public IReadOnlyList<string>? Errors { get; set; }
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }
}
