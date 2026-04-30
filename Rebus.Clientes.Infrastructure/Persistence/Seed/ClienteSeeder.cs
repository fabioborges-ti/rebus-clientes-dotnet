using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rebus.Clientes.Domain.Entities;

namespace Rebus.Clientes.Infrastructure.Persistence.Seed;

public static class ClienteSeeder
{
    public static async Task SeedAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Clientes.AnyAsync())
            return;

        var faker = new Faker("pt_BR");
        var clientes = new List<Cliente>();
        var emailsUsados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var documentosUsados = new HashSet<string>();

        while (clientes.Count < 10)
        {
            var nome = $"{faker.Name.FirstName()} {faker.Name.LastName()} {faker.Name.LastName()}";
            if (nome.Length < 10)
                continue;

            var email = faker.Internet.Email(nome.Split(' ')[0], nome.Split(' ')[1]).ToLowerInvariant();
            if (!emailsUsados.Add(email))
                continue;

            var cpf = GerarCpfValido(faker);
            if (!documentosUsados.Add(cpf))
                continue;

            clientes.Add(new Cliente(Guid.NewGuid(), nome, email, cpf));
        }

        await context.Clientes.AddRangeAsync(clientes);
        await context.SaveChangesAsync();

        logger.LogInformation("Seed concluído: {Total} clientes inseridos.", clientes.Count);
    }

    private static string GerarCpfValido(Faker f)
    {
        var n = Enumerable.Range(0, 9).Select(_ => f.Random.Int(0, 9)).ToArray();

        var d1 = 11 - (Enumerable.Range(0, 9).Sum(i => n[i] * (10 - i)) % 11);
        d1 = d1 >= 10 ? 0 : d1;

        var d2 = 11 - ((Enumerable.Range(0, 9).Sum(i => n[i] * (11 - i)) + d1 * 2) % 11);
        d2 = d2 >= 10 ? 0 : d2;

        return string.Concat(n) + d1 + d2;
    }
}
