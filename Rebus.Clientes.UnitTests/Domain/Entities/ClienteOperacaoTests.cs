using FluentAssertions;
using Rebus.Clientes.Domain.Entities;
using Rebus.Clientes.Domain.Enums;

namespace Rebus.Clientes.UnitTests.Domain.Entities;

public class ClienteOperacaoTests
{
    [Fact]
    public void Constructor_Criacao_DeveInicializarComEstadoPendente()
    {
        var correlationId = Guid.NewGuid();

        var operacao = new ClienteOperacao(correlationId, OperacaoTipo.Criacao);

        operacao.CorrelationId.Should().Be(correlationId);
        operacao.Tipo.Should().Be(OperacaoTipo.Criacao);
        operacao.Estado.Should().Be(OperacaoEstado.Pendente);
        operacao.ClienteId.Should().BeNull();
        operacao.MensagemErro.Should().BeNull();
        operacao.CriadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        operacao.AtualizadoEmUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_Atualizacao_DevePersistirClienteId()
    {
        var correlationId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();

        var operacao = new ClienteOperacao(correlationId, OperacaoTipo.Atualizacao, clienteId);

        operacao.Tipo.Should().Be(OperacaoTipo.Atualizacao);
        operacao.ClienteId.Should().Be(clienteId);
        operacao.Estado.Should().Be(OperacaoEstado.Pendente);
    }

    [Fact]
    public void Constructor_Atualizacao_SemClienteId_DeveLancarArgumentException()
    {
        var act = () => new ClienteOperacao(Guid.NewGuid(), OperacaoTipo.Atualizacao, null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*clienteId*");
    }

    [Fact]
    public void Constructor_Criacao_ComClienteIdOpcional_NaoDeveLancarExcecao()
    {
        var act = () => new ClienteOperacao(Guid.NewGuid(), OperacaoTipo.Criacao, null);

        act.Should().NotThrow();
    }

    [Fact]
    public void MarcarConcluida_DeveAlterarEstadoClienteIdELimparErro()
    {
        var operacao = new ClienteOperacao(Guid.NewGuid(), OperacaoTipo.Criacao);
        var clienteId = Guid.NewGuid();

        operacao.MarcarConcluida(clienteId);

        operacao.Estado.Should().Be(OperacaoEstado.Concluida);
        operacao.ClienteId.Should().Be(clienteId);
        operacao.MensagemErro.Should().BeNull();
        operacao.AtualizadoEmUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarcarFalha_DeveAlterarEstadoERegistrarMensagem()
    {
        var operacao = new ClienteOperacao(Guid.NewGuid(), OperacaoTipo.Criacao);

        operacao.MarcarFalha("Erro de teste simulado");

        operacao.Estado.Should().Be(OperacaoEstado.Falhou);
        operacao.MensagemErro.Should().Be("Erro de teste simulado");
        operacao.AtualizadoEmUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarcarFalha_DeveAtualizarTimestampDeAtualizacao()
    {
        var operacao = new ClienteOperacao(Guid.NewGuid(), OperacaoTipo.Criacao);
        var antesDeMarcar = operacao.AtualizadoEmUtc;

        operacao.MarcarFalha("Qualquer falha");

        operacao.AtualizadoEmUtc.Should().BeOnOrAfter(antesDeMarcar);
    }

    [Fact]
    public void MarcarConcluida_DeveAtualizarTimestampDeAtualizacao()
    {
        var operacao = new ClienteOperacao(Guid.NewGuid(), OperacaoTipo.Criacao);
        var antesDeMarcar = operacao.AtualizadoEmUtc;

        operacao.MarcarConcluida(Guid.NewGuid());

        operacao.AtualizadoEmUtc.Should().BeOnOrAfter(antesDeMarcar);
    }
}
