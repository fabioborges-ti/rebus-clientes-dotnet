using Rebus.Clientes.Application.Abstractions.Correlation;

namespace Rebus.Clientes.Infrastructure.Correlation;

/// <summary>
/// Implementação de <see cref="ICorrelationIdAccessor"/> que gera um novo CorrelationId
/// por escopo (scoped lifetime) e o mantém.
/// </summary>
public class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly Guid _correlationId;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CorrelationIdAccessor"/>, gerando um novo CorrelationId.
    /// </summary>
    public CorrelationIdAccessor()
    {
        _correlationId = Guid.NewGuid();
    }

    /// <inheritdoc />
    public Guid GetCorrelationId() => _correlationId;
}