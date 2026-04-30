using Microsoft.EntityFrameworkCore;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Domain.Entities;

namespace Rebus.Clientes.Infrastructure.Persistence.Repositories;

public class ClienteOperacaoRepository : IClienteOperacaoRepository
{
    private readonly AppDbContext _dbContext;

    public ClienteOperacaoRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ClienteOperacao operacao, CancellationToken cancellationToken)
    {
        await _dbContext.ClienteOperacoes.AddAsync(operacao, cancellationToken);
    }

    public async Task<ClienteOperacao?> GetByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ClienteOperacoes
            .FirstOrDefaultAsync(x => x.CorrelationId == correlationId, cancellationToken);
    }
}
