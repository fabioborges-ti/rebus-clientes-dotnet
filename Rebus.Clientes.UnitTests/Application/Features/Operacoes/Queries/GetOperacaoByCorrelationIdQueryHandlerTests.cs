using FluentAssertions;
using Moq;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Features.Operacoes.Queries.GetOperacaoByCorrelationId;
using Rebus.Clientes.Domain.Entities;
using Rebus.Clientes.Domain.Enums;

namespace Rebus.Clientes.UnitTests.Application.Features.Operacoes.Queries;

public class GetOperacaoByCorrelationIdQueryHandlerTests
{
    private readonly Mock<IClienteOperacaoRepository> _repoMock = new();
    private readonly GetOperacaoByCorrelationIdQueryHandler _handler;

    public GetOperacaoByCorrelationIdQueryHandlerTests()
    {
        _handler = new GetOperacaoByCorrelationIdQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_OperacaoEncontrada_DeveRetornarDtoPreenchido()
    {
        var correlationId = Guid.NewGuid();
        var operacao = new ClienteOperacao(correlationId, OperacaoTipo.Criacao);
        _repoMock.Setup(r => r.GetByCorrelationIdAsync(correlationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(operacao);

        var result = await _handler.Handle(new GetOperacaoByCorrelationIdQuery(correlationId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.CorrelationId.Should().Be(correlationId);
        result.Tipo.Should().Be(OperacaoTipo.Criacao);
        result.Estado.Should().Be(OperacaoEstado.Pendente);
        result.ClienteId.Should().BeNull();
        result.MensagemErro.Should().BeNull();
    }

    [Fact]
    public async Task Handle_OperacaoNaoEncontrada_DeveRetornarNull()
    {
        var correlationId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByCorrelationIdAsync(correlationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClienteOperacao?)null);

        var result = await _handler.Handle(new GetOperacaoByCorrelationIdQuery(correlationId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_OperacaoConcluida_DeveRetornarDtoComClienteId()
    {
        var correlationId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var operacao = new ClienteOperacao(correlationId, OperacaoTipo.Criacao);
        operacao.MarcarConcluida(clienteId);

        _repoMock.Setup(r => r.GetByCorrelationIdAsync(correlationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(operacao);

        var result = await _handler.Handle(new GetOperacaoByCorrelationIdQuery(correlationId), CancellationToken.None);

        result!.Estado.Should().Be(OperacaoEstado.Concluida);
        result.ClienteId.Should().Be(clienteId);
        result.MensagemErro.Should().BeNull();
    }

    [Fact]
    public async Task Handle_OperacaoFalhou_DeveRetornarDtoComMensagemDeErro()
    {
        var correlationId = Guid.NewGuid();
        var operacao = new ClienteOperacao(correlationId, OperacaoTipo.Criacao);
        operacao.MarcarFalha("Email duplicado detectado no Worker");

        _repoMock.Setup(r => r.GetByCorrelationIdAsync(correlationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(operacao);

        var result = await _handler.Handle(new GetOperacaoByCorrelationIdQuery(correlationId), CancellationToken.None);

        result!.Estado.Should().Be(OperacaoEstado.Falhou);
        result.MensagemErro.Should().Be("Email duplicado detectado no Worker");
    }

    [Fact]
    public async Task Handle_OperacaoAtualizacao_DeveRetornarTipoCorreto()
    {
        var correlationId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var operacao = new ClienteOperacao(correlationId, OperacaoTipo.Atualizacao, clienteId);

        _repoMock.Setup(r => r.GetByCorrelationIdAsync(correlationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(operacao);

        var result = await _handler.Handle(new GetOperacaoByCorrelationIdQuery(correlationId), CancellationToken.None);

        result!.Tipo.Should().Be(OperacaoTipo.Atualizacao);
        result.ClienteId.Should().Be(clienteId);
    }

    [Fact]
    public async Task Handle_DevePassarCorrelationIdCorretoPararBusca()
    {
        var correlationId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByCorrelationIdAsync(correlationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClienteOperacao?)null);

        await _handler.Handle(new GetOperacaoByCorrelationIdQuery(correlationId), CancellationToken.None);

        _repoMock.Verify(r => r.GetByCorrelationIdAsync(correlationId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
