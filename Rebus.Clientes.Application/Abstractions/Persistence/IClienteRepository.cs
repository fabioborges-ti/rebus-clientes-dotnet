using Rebus.Clientes.Domain.Entities;

namespace Rebus.Clientes.Application.Abstractions.Persistence;

public interface IClienteRepository
{
    Task AddAsync(Cliente cliente, CancellationToken cancellationToken);
    Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Cliente>> GetAllAsync(CancellationToken cancellationToken);
    Task<(IReadOnlyList<Cliente> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> ExistsByDocumentoAsync(string documento, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailExceptIdAsync(Guid id, string email, CancellationToken cancellationToken);
    Task<bool> ExistsByDocumentoExceptIdAsync(Guid id, string documento, CancellationToken cancellationToken);
    void Update(Cliente cliente);
    void Remove(Cliente cliente);
}
