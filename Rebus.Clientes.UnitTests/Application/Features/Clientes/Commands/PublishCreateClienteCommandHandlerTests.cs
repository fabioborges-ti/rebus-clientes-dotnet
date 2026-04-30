using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Rebus.Clientes.Application.Abstractions.Messaging;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Dtos;
using Rebus.Clientes.Application.Features.Clientes.Commands.PublishCreateCliente;
using Rebus.Clientes.Application.Messaging;
using Rebus.Clientes.Application.Validators;
using Rebus.Clientes.Domain.Entities;
using Rebus.Clientes.Domain.Enums;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.UnitTests.Application.Features.Clientes.Commands;

public class PublishCreateClienteCommandHandlerTests
{
    private readonly Mock<IClienteRepository> _repoMock = new();
    private readonly Mock<IClienteOperacaoRepository> _operacaoRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IClienteMessageBus> _messageBusMock = new();
    private readonly IValidator<ClienteWriteDto> _validator;
    private readonly PublishCreateClienteCommandHandler _handler;

    private const string CpfValido = "52998224725";

    public PublishCreateClienteCommandHandlerTests()
    {
        _validator = new CreateClienteDtoValidator();
        _handler = new PublishCreateClienteCommandHandler(
            _repoMock.Object,
            _operacaoRepoMock.Object,
            _uowMock.Object,
            _messageBusMock.Object,
            _validator,
            NullLogger<PublishCreateClienteCommandHandler>.Instance);
    }

    private ClienteWriteDto DtoValido() => new()
    {
        Nome = "João da Silva Santos",
        Email = "joao@email.com",
        Documento = CpfValido
    };

    private void ConfigurarSemConflitos()
    {
        _repoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByDocumentoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task Handle_DadosValidos_DeveRetornarCorrelationIdNaoVazio()
    {
        ConfigurarSemConflitos();

        var correlationId = await _handler.Handle(new PublishCreateClienteCommand(DtoValido()), CancellationToken.None);

        correlationId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_DadosValidos_DevePersistirOperacaoPendenteEPublicarMensagem()
    {
        ConfigurarSemConflitos();

        await _handler.Handle(new PublishCreateClienteCommand(DtoValido()), CancellationToken.None);

        _operacaoRepoMock.Verify(r => r.AddAsync(
            It.Is<ClienteOperacao>(o => o.Estado == OperacaoEstado.Pendente),
            It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _messageBusMock.Verify(b => b.PublishAsync(It.IsAny<CreateClienteMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DadosValidos_DevePersistirOperacaoAntesDePublicar()
    {
        ConfigurarSemConflitos();
        var ordem = new List<string>();

        _operacaoRepoMock.Setup(r => r.AddAsync(It.IsAny<ClienteOperacao>(), It.IsAny<CancellationToken>()))
            .Callback(() => ordem.Add("AddOperacao"))
            .Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => ordem.Add("SaveChanges"))
            .ReturnsAsync(1);
        _messageBusMock.Setup(b => b.PublishAsync(It.IsAny<CreateClienteMessage>(), It.IsAny<CancellationToken>()))
            .Callback(() => ordem.Add("Publish"))
            .Returns(Task.CompletedTask);

        await _handler.Handle(new PublishCreateClienteCommand(DtoValido()), CancellationToken.None);

        ordem.Should().ContainInOrder("AddOperacao", "SaveChanges", "Publish");
    }

    [Fact]
    public async Task Handle_EmailJaCadastrado_DeveLancarConflictExceptionSemPublicar()
    {
        _repoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _handler.Handle(new PublishCreateClienteCommand(DtoValido()), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _messageBusMock.Verify(b => b.PublishAsync(It.IsAny<CreateClienteMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DocumentoJaCadastrado_DeveLancarConflictExceptionSemPublicar()
    {
        _repoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByDocumentoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _handler.Handle(new PublishCreateClienteCommand(DtoValido()), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _messageBusMock.Verify(b => b.PublishAsync(It.IsAny<CreateClienteMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DadosInvalidos_DeveLancarValidationExceptionSemConsultarBanco()
    {
        var dto = DtoValido();
        dto.Nome = "x";

        var act = () => _handler.Handle(new PublishCreateClienteCommand(dto), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _repoMock.Verify(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _messageBusMock.Verify(b => b.PublishAsync(It.IsAny<CreateClienteMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DadosValidos_DeveMandarmEmailNormalizadoNaMensagem()
    {
        ConfigurarSemConflitos();
        CreateClienteMessage? mensagemCapturada = null;

        _messageBusMock.Setup(b => b.PublishAsync(It.IsAny<CreateClienteMessage>(), It.IsAny<CancellationToken>()))
            .Callback<CreateClienteMessage, CancellationToken>((msg, _) => mensagemCapturada = msg)
            .Returns(Task.CompletedTask);

        var dto = DtoValido();
        dto.Email = "JOAO@EMAIL.COM";

        await _handler.Handle(new PublishCreateClienteCommand(dto), CancellationToken.None);

        mensagemCapturada!.Email.Should().Be("joao@email.com");
    }

    [Fact]
    public async Task Handle_DadosValidos_CorrelationIdDaMensagemDeveSerIgualAoRetornado()
    {
        ConfigurarSemConflitos();
        CreateClienteMessage? mensagemCapturada = null;

        _messageBusMock.Setup(b => b.PublishAsync(It.IsAny<CreateClienteMessage>(), It.IsAny<CancellationToken>()))
            .Callback<CreateClienteMessage, CancellationToken>((msg, _) => mensagemCapturada = msg)
            .Returns(Task.CompletedTask);

        var correlationId = await _handler.Handle(new PublishCreateClienteCommand(DtoValido()), CancellationToken.None);

        mensagemCapturada!.CorrelationId.Should().Be(correlationId);
    }
}
