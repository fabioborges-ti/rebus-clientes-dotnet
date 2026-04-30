using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Rebus.Clientes.Application.Abstractions.Messaging;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Dtos;
using Rebus.Clientes.Application.Features.Clientes.Commands.PublishUpdateCliente;
using Rebus.Clientes.Application.Messaging;
using Rebus.Clientes.Application.Validators;
using Rebus.Clientes.Domain.Entities;
using Rebus.Clientes.Domain.Enums;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.UnitTests.Application.Features.Clientes.Commands;

public class PublishUpdateClienteCommandHandlerTests
{
    private readonly Mock<IClienteRepository> _repoMock = new();
    private readonly Mock<IClienteOperacaoRepository> _operacaoRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IClienteMessageBus> _messageBusMock = new();
    private readonly IValidator<ClienteWriteDto> _validator;
    private readonly PublishUpdateClienteCommandHandler _handler;

    private const string CpfValido = "52998224725";
    private const string CpfValido2 = "11144477735";

    public PublishUpdateClienteCommandHandlerTests()
    {
        _validator = new CreateClienteDtoValidator();
        _handler = new PublishUpdateClienteCommandHandler(
            _repoMock.Object,
            _operacaoRepoMock.Object,
            _uowMock.Object,
            _messageBusMock.Object,
            _validator,
            NullLogger<PublishUpdateClienteCommandHandler>.Instance);
    }

    private static Cliente ClienteExistente(Guid id) =>
        new(id, "Nome Original Antigo", "original@email.com", CpfValido);

    private ClienteWriteDto DtoValido() => new()
    {
        Nome = "Nome Atualizado Completo",
        Email = "novo@email.com",
        Documento = CpfValido2
    };

    private void ConfigurarClienteExistenteSemConflitos(Guid id)
    {
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ClienteExistente(id));
        _repoMock.Setup(r => r.ExistsByEmailExceptIdAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByDocumentoExceptIdAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task Handle_DadosValidos_DeveRetornarCorrelationIdNaoVazio()
    {
        var id = Guid.NewGuid();
        ConfigurarClienteExistenteSemConflitos(id);

        var correlationId = await _handler.Handle(new PublishUpdateClienteCommand(id, DtoValido()), CancellationToken.None);

        correlationId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_DadosValidos_DevePersistirOperacaoComClienteIdEPublicar()
    {
        var id = Guid.NewGuid();
        ConfigurarClienteExistenteSemConflitos(id);

        await _handler.Handle(new PublishUpdateClienteCommand(id, DtoValido()), CancellationToken.None);

        _operacaoRepoMock.Verify(r => r.AddAsync(
            It.Is<ClienteOperacao>(o =>
                o.Estado == OperacaoEstado.Pendente &&
                o.Tipo == OperacaoTipo.Atualizacao &&
                o.ClienteId == id),
            It.IsAny<CancellationToken>()), Times.Once);
        _messageBusMock.Verify(b => b.PublishAsync(It.IsAny<UpdateClienteMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ClienteNaoEncontrado_DeveLancarNotFoundExceptionSemPublicar()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Cliente?)null);

        var act = () => _handler.Handle(new PublishUpdateClienteCommand(id, DtoValido()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _messageBusMock.Verify(b => b.PublishAsync(It.IsAny<UpdateClienteMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailEmUso_DeveLancarConflictExceptionSemPublicar()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(ClienteExistente(id));
        _repoMock.Setup(r => r.ExistsByEmailExceptIdAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _handler.Handle(new PublishUpdateClienteCommand(id, DtoValido()), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _messageBusMock.Verify(b => b.PublishAsync(It.IsAny<UpdateClienteMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DocumentoEmUso_DeveLancarConflictExceptionSemPublicar()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(ClienteExistente(id));
        _repoMock.Setup(r => r.ExistsByEmailExceptIdAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByDocumentoExceptIdAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _handler.Handle(new PublishUpdateClienteCommand(id, DtoValido()), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _messageBusMock.Verify(b => b.PublishAsync(It.IsAny<UpdateClienteMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailEDocumentoEmUso_DeveLancarConflictExceptionComMultiplosErros()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(ClienteExistente(id));
        _repoMock.Setup(r => r.ExistsByEmailExceptIdAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repoMock.Setup(r => r.ExistsByDocumentoExceptIdAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _handler.Handle(new PublishUpdateClienteCommand(id, DtoValido()), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Errors.Should().HaveCount(2);
        exception.Which.Errors.Should().Contain("E-mail já pertence a outro cliente.");
        exception.Which.Errors.Should().Contain("Documento já pertence a outro cliente.");
        _messageBusMock.Verify(b => b.PublishAsync(It.IsAny<UpdateClienteMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DadosInvalidos_DeveLancarValidationExceptionSemConsultarBanco()
    {
        var dto = DtoValido();
        dto.Nome = "x";

        var act = () => _handler.Handle(new PublishUpdateClienteCommand(Guid.NewGuid(), dto), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DadosValidos_MensagemDeveConterClienteIdEEmailNormalizado()
    {
        var id = Guid.NewGuid();
        ConfigurarClienteExistenteSemConflitos(id);
        UpdateClienteMessage? mensagemCapturada = null;

        _messageBusMock.Setup(b => b.PublishAsync(It.IsAny<UpdateClienteMessage>(), It.IsAny<CancellationToken>()))
            .Callback<UpdateClienteMessage, CancellationToken>((msg, _) => mensagemCapturada = msg)
            .Returns(Task.CompletedTask);

        var dto = DtoValido();
        dto.Email = "NOVO@EMAIL.COM";

        await _handler.Handle(new PublishUpdateClienteCommand(id, dto), CancellationToken.None);

        mensagemCapturada!.ClienteId.Should().Be(id);
        mensagemCapturada.Email.Should().Be("novo@email.com");
    }

    [Fact]
    public async Task Handle_DadosValidos_CorrelationIdDaMensagemDeveSerIgualAoRetornado()
    {
        var id = Guid.NewGuid();
        ConfigurarClienteExistenteSemConflitos(id);
        UpdateClienteMessage? mensagemCapturada = null;

        _messageBusMock.Setup(b => b.PublishAsync(It.IsAny<UpdateClienteMessage>(), It.IsAny<CancellationToken>()))
            .Callback<UpdateClienteMessage, CancellationToken>((msg, _) => mensagemCapturada = msg)
            .Returns(Task.CompletedTask);

        var correlationId = await _handler.Handle(new PublishUpdateClienteCommand(id, DtoValido()), CancellationToken.None);

        mensagemCapturada!.CorrelationId.Should().Be(correlationId);
    }
}
