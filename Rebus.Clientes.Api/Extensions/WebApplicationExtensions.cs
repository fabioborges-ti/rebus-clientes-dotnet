using Microsoft.EntityFrameworkCore;
using Rebus.Clientes.Api.Middlewares;
using Rebus.Clientes.Infrastructure.Persistence;
using Rebus.Clientes.Infrastructure.Persistence.Seed;

namespace Rebus.Clientes.Api.Extensions;

/// <summary>
/// Configuração do pipeline HTTP e da base de dados na inicialização da aplicação.
/// </summary>
public static class WebApplicationExtensions
{
    private const int MigrationMaxRetries = 8;
    private static readonly TimeSpan MigrationRetryDelay = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Aplica migrations pendentes do EF Core com retry, tolerando atrasos na inicialização do PostgreSQL.
    /// Executa o seed inicial de clientes caso o banco esteja vazio.
    /// </summary>
    public static async Task<WebApplication> ApplyDatabaseMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        for (var attempt = 1; attempt <= MigrationMaxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("Aplicando migrations (tentativa {Attempt}/{Max})...", attempt, MigrationMaxRetries);
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Migrations aplicadas com sucesso.");
                await ClienteSeeder.SeedAsync(dbContext, logger);
                return app;
            }
            catch (Exception ex) when (attempt < MigrationMaxRetries)
            {
                logger.LogWarning(ex, "Falha ao aplicar migrations. Nova tentativa em {Delay}s...", MigrationRetryDelay.TotalSeconds);
                await Task.Delay(MigrationRetryDelay);
            }
        }

        // Última tentativa sem capturar exceção — deixa o app encerrar com log claro
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await ctx.Database.MigrateAsync();
        await ClienteSeeder.SeedAsync(ctx, logger);
        return app;
    }

    /// <summary>
    /// Configura Swagger, redirecionamentos de conveniência, tratamento global de exceções, autorização e endpoints MVC.
    /// </summary>
    public static WebApplication ConfigureHttpPipeline(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseSwagger();
        app.UseSwaggerUI();
        MapSwaggerRedirects(app);
        app.MapControllers();
        app.MapServiceHealthEndpoints();
        return app;
    }

    private static void MapSwaggerRedirects(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
        endpoints.MapGet("/swagger/index", () => Results.Redirect("/swagger/index.html")).ExcludeFromDescription();
    }
}
