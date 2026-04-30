using AutoMapper;
using Microsoft.OpenApi.Models;
using Rebus.Clientes.Api.Mapping;
using Rebus.Clientes.Application;
using Rebus.Clientes.Application.Mapping;
using Rebus.Clientes.Application.Abstractions.Messaging;
using Rebus.Clientes.Infrastructure;
using Rebus.Clientes.Infrastructure.Messaging;
using Rebus.Config;
using Rebus.ServiceProvider;

namespace Rebus.Clientes.Api.Extensions;

/// <summary>
/// Registro de serviços específicos da camada API (apresentação, integrações e mensageria).
/// </summary>
public static class ServiceCollectionExtensions
{
    private static class ConnectionStrings
    {
        public const string RabbitMq = "RabbitMq";
    }

    /// <summary>
    /// Adiciona controllers, documentação OpenAPI/Swagger, AutoMapper, Application, Infrastructure e Rebus como publicador one-way.
    /// </summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Rebus.Clientes API",
                Version = "v1",
                Description =
                    "API de referência em .NET 8 com Clean Architecture, CQRS, EF Core, PostgreSQL, Rebus e RabbitMQ. " +
                    "Criação e atualização de clientes via fila assíncrona (`202 Accepted`)."
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        });
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(typeof(ApiMappingProfile).Assembly);
            cfg.AddMaps(typeof(ApplicationMappingProfile).Assembly);
        });
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddRebusPublisher(configuration);
        services.AddScoped<IClienteMessageBus, ClienteMessageBus>();
        services.AddServiceHealthChecks(configuration);
        return services;
    }

    /// <summary>
    /// Registra o Rebus como cliente RabbitMQ apenas para publicação (sem consumo neste processo).
    /// </summary>
    public static IServiceCollection AddRebusPublisher(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitConnection = configuration.GetConnectionString(ConnectionStrings.RabbitMq)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStrings.RabbitMq}' não encontrada.");

        services.AddRebus(configure => configure
            .Transport(t => t.UseRabbitMqAsOneWayClient(rabbitConnection)));

        return services;
    }
}
