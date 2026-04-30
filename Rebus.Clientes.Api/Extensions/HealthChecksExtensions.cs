using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Rebus.Clientes.Api.Extensions;

internal static class HealthChecksExtensions
{
    internal static class HealthConnectionStrings
    {
        public const string Postgres = "Postgres";
        public const string RabbitMq = "RabbitMq";
    }

    /// <summary>
    /// Health checks: PostgreSQL, RabbitMQ e autoavaliação do processo da API.
    /// </summary>
    public static IServiceCollection AddServiceHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var postgres = configuration.GetConnectionString(HealthConnectionStrings.Postgres)
            ?? throw new InvalidOperationException($"Connection string '{HealthConnectionStrings.Postgres}' não encontrada.");
        var rabbit = configuration.GetConnectionString(HealthConnectionStrings.RabbitMq)
            ?? throw new InvalidOperationException($"Connection string '{HealthConnectionStrings.RabbitMq}' não encontrada.");

        services.AddHealthChecks()
            .AddNpgSql(postgres, name: "postgresql", tags: ["db", "ready"])
            .AddRabbitMQ(rabbitConnectionString: rabbit, name: "rabbitmq", tags: ["messaging", "ready"])
            .AddCheck("api", () => HealthCheckResult.Healthy("Processo em execução."), tags: ["self", "live"]);

        services.AddHealthChecksUI(settings =>
        {
            settings.SetEvaluationTimeInSeconds(10);
            settings.MaximumHistoryEntriesPerEndpoint(50);
            settings.AddHealthCheckEndpoint("Rebus.Clientes API", "/health");
        }).AddInMemoryStorage();

        return services;
    }

    /// <summary>
    /// Expõe <c>/health</c> no formato do Health Checks UI e o dashboard em <c>/health-ui</c>.
    /// </summary>
    public static WebApplication MapServiceHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecksUI(options =>
        {
            options.UIPath = "/health-ui";
            options.PageTitle = "Monitoramento — Rebus.Clientes";
        });

        return app;
    }
}
