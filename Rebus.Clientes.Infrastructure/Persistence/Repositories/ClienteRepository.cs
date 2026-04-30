using Microsoft.EntityFrameworkCore;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Domain.Entities;

namespace Rebus.Clientes.Infrastructure.Persistence.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _dbContext;

    public ClienteRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Cliente cliente, CancellationToken cancellationToken)
    {
        await _dbContext.Clientes.AddAsync(cliente, cancellationToken);
    }

    public async Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Clientes
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Cliente>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Clientes
            .AsNoTracking()
            .OrderByDescending(x => x.CriadoEm)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Cliente> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Clientes
            .AsNoTracking()
            .OrderByDescending(x => x.CriadoEm);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await _dbContext.Clientes.AnyAsync(x => x.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByDocumentoAsync(string documento, CancellationToken cancellationToken)
    {
        return await _dbContext.Clientes.AnyAsync(x => x.Documento == documento, cancellationToken);
    }

    public async Task<bool> ExistsByEmailExceptIdAsync(Guid id, string email, CancellationToken cancellationToken)
    {
        return await _dbContext.Clientes.AnyAsync(x => x.Id != id && x.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByDocumentoExceptIdAsync(Guid id, string documento, CancellationToken cancellationToken)
    {
        return await _dbContext.Clientes.AnyAsync(x => x.Id != id && x.Documento == documento, cancellationToken);
    }

    public void Update(Cliente cliente)
    {
        _dbContext.Clientes.Update(cliente);
    }

    public void Remove(Cliente cliente)
    {
        _dbContext.Clientes.Remove(cliente);
    }
}
