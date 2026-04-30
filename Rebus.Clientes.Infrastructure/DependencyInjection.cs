using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Clientes.Application.Abstractions.Correlation;
using Rebus.Clientes.Application.Abstractions.Messaging;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Infrastructure.Correlation;
using Rebus.Clientes.Infrastructure.Messaging;
using Rebus.Clientes.Infrastructure.Persistence;
using Rebus.Clientes.Infrastructure.Persistence.Repositories;

namespace Rebus.Clientes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IClienteOperacaoRepository, ClienteOperacaoRepository>();
        services.AddScoped<IUnitOfWork, AppDbContext>();
        services.AddScoped<IClienteMessageBus, ClienteMessageBus>();
        services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>(); // Adicionado aqui

        return services;
    }
}