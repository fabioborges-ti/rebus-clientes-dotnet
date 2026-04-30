using Rebus.Clientes.Domain.Entities;

namespace Rebus.Clientes.Application.Abstractions.Persistence;

public interface IClienteOperacaoRepository
{
    Task AddAsync(ClienteOperacao operacao, CancellationToken cancellationToken);
    Task<ClienteOperacao?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken);
}
