namespace Rebus.Clientes.Api.Models;

/// <summary>
/// Metadados de paginação retornados como objeto composto na resposta da API.
/// </summary>
public class PaginacaoMetadata
{
    /// <summary>Página atual (começa em 1).</summary>
    public int Page { get; init; }

    /// <summary>Quantidade de registros solicitada por página.</summary>
    public int PageSize { get; init; }

    /// <summary>Total de registros existentes na base.</summary>
    public int TotalRegistros { get; init; }

    /// <summary>Total de páginas disponíveis.</summary>
    public int TotalPaginas { get; init; }
}
