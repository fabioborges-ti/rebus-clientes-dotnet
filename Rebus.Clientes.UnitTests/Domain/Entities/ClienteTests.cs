using FluentAssertions;
using Rebus.Clientes.Domain.Entities;

namespace Rebus.Clientes.UnitTests.Domain.Entities;

public class ClienteTests
{
    private const string CpfValido = "52998224725";

    [Fact]
    public void Constructor_DeveInicializarPropriedadesCorretamente()
    {
        var id = Guid.NewGuid();

        var cliente = new Cliente(id, "João da Silva Santos", "joao@email.com", CpfValido);

        cliente.Id.Should().Be(id);
        cliente.Nome.Should().Be("João da Silva Santos");
        cliente.Email.Should().Be("joao@email.com");
        cliente.Documento.Should().Be(CpfValido);
        cliente.CriadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_DeveNormalizarEmailParaMinusculo()
    {
        var cliente = new Cliente(Guid.NewGuid(), "Nome Completo Teste", "JOAO@EMAIL.COM", CpfValido);

        cliente.Email.Should().Be("joao@email.com");
    }

    [Fact]
    public void Constructor_DeveRemoverEspacosDoNome()
    {
        var cliente = new Cliente(Guid.NewGuid(), "  Nome Com Espacos  ", "teste@email.com", CpfValido);

        cliente.Nome.Should().Be("Nome Com Espacos");
    }

    [Fact]
    public void Constructor_DeveRemoverEspacosDoEmail()
    {
        var cliente = new Cliente(Guid.NewGuid(), "Nome Completo Teste", "  joao@email.com  ", CpfValido);

        cliente.Email.Should().Be("joao@email.com");
    }

    [Fact]
    public void Constructor_DeveRemoverEspacosDoDocumento()
    {
        var cliente = new Cliente(Guid.NewGuid(), "Nome Completo Teste", "joao@email.com", $"  {CpfValido}  ");

        cliente.Documento.Should().Be(CpfValido);
    }

    [Fact]
    public void Atualizar_DeveAlterarPropriedadesCorretamente()
    {
        var cliente = new Cliente(Guid.NewGuid(), "Nome Original Longo", "original@email.com", CpfValido);

        cliente.Atualizar("Nome Atualizado Longo", "NOVO@EMAIL.COM", "11144477735");

        cliente.Nome.Should().Be("Nome Atualizado Longo");
        cliente.Email.Should().Be("novo@email.com");
        cliente.Documento.Should().Be("11144477735");
    }

    [Fact]
    public void Atualizar_NaoDeveAlterarIdNemCriadoEm()
    {
        var id = Guid.NewGuid();
        var cliente = new Cliente(id, "Nome Original Longo", "original@email.com", CpfValido);
        var criadoEm = cliente.CriadoEm;

        cliente.Atualizar("Nome Atualizado Longo", "novo@email.com", "11144477735");

        cliente.Id.Should().Be(id);
        cliente.CriadoEm.Should().Be(criadoEm);
    }

    [Theory]
    [InlineData("  email@teste.com  ", "email@teste.com")]
    [InlineData("EMAIL@TESTE.COM", "email@teste.com")]
    [InlineData("  EMAIL@TESTE.COM  ", "email@teste.com")]
    public void Atualizar_DeveNormalizarEmail(string emailEntrada, string emailEsperado)
    {
        var cliente = new Cliente(Guid.NewGuid(), "Nome Para Teste Agora", "original@email.com", CpfValido);

        cliente.Atualizar("Nome Para Teste Agora", emailEntrada, CpfValido);

        cliente.Email.Should().Be(emailEsperado);
    }
}
