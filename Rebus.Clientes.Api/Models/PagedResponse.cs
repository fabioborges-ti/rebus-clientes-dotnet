namespace Rebus.Clientes.Api.Models;

/// <summary>
/// Envelope de resposta paginada: dados da página atual + metadados de paginação como objeto composto.
/// </summary>
public class PagedResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>Itens da página atual.</summary>
    public IReadOnlyList<T> Data { get; init; } = [];

    /// <summary>Metadados de paginação (página, tamanho, totais).</summary>
    public PaginacaoMetadata Paginacao { get; init; } = new();
}
