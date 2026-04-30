namespace Rebus.Clientes.Application.Dtos;

/// <summary>
/// Resultado paginado genérico retornado pelos handlers de consulta.
/// </summary>
public class PagedResultDto<T>
{
    /// <summary>Itens da página atual.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>Número da página atual (começa em 1).</summary>
    public int Page { get; init; }

    /// <summary>Quantidade de registros por página.</summary>
    public int PageSize { get; init; }

    /// <summary>Total de registros existentes (sem filtro de paginação).</summary>
    public int TotalRegistros { get; init; }

    /// <summary>Total de páginas calculado com base em <see cref="TotalRegistros"/> e <see cref="PageSize"/>.</summary>
    public int TotalPaginas => PageSize > 0
        ? (int)Math.Ceiling(TotalRegistros / (double)PageSize)
        : 0;
}
