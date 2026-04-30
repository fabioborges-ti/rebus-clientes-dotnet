using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Infrastructure.Persistence;
using Rebus.Clientes.Infrastructure.Persistence.Repositories;

namespace Rebus.Clientes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' não encontrada.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IClienteOperacaoRepository, ClienteOperacaoRepository>();

        return services;
    }
}
