using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Moq;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Dtos;
using Rebus.Clientes.Application.Features.Clientes.Commands.UpdateCliente;
using Rebus.Clientes.Application.Mapping;
using Rebus.Clientes.Application.Validators;
using Rebus.Clientes.Domain.Entities;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.UnitTests.Application.Features.Clientes.Commands;

public class UpdateClienteCommandHandlerTests
{
    private readonly Mock<IClienteRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly IMapper _mapper;
    private readonly IValidator<ClienteWriteDto> _validator;
    private readonly UpdateClienteCommandHandler _handler;

    private const string CpfValido = "52998224725";
    private const string CpfValido2 = "11144477735";

    public UpdateClienteCommandHandlerTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(ApplicationMappingProfile).Assembly));
        _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();
        _validator = new CreateClienteDtoValidator();
        _handler = new UpdateClienteCommandHandler(_repoMock.Object, _uowMock.Object, _mapper, _validator);
    }

    private static Cliente ClienteExistente(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), "Nome Original Antigo", "original@email.com", CpfValido);

    private ClienteWriteDto DtoValido() => new()
    {
        Nome = "Nome Atualizado Longo",
        Email = "atualizado@email.com",
        Documento = CpfValido2
    };

    [Fact]
    public async Task Handle_DadosValidos_DeveAtualizarERetornarDto()
    {
        var id = Guid.NewGuid();
        var entity = ClienteExistente(id);

        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _repoMock.Setup(r => r.ExistsByEmailExceptIdAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByDocumentoExceptIdAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _handler.Handle(new UpdateClienteCommand(id, DtoValido()), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Nome.Should().Be("Nome Atualizado Longo");
        result.Email.Should().Be("atualizado@email.com");
        result.Documento.Should().Be(CpfValido2);
    }

    [Fact]
    public async Task Handle_DadosValidos_DeveInvocarUpdateESaveChanges()
    {
        var id = Guid.NewGuid();
        var entity = ClienteExistente(id);

        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _repoMock.Setup(r => r.ExistsByEmailExceptIdAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByDocumentoExceptIdAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await _handler.Handle(new UpdateClienteCommand(id, DtoValido()), CancellationToken.None);

        _repoMock.Verify(r => r.Update(It.IsAny<Cliente>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ClienteNaoEncontrado_DeveLancarNotFoundException()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Cliente?)null);

        var act = () => _handler.Handle(new UpdateClienteCommand(id, DtoValido()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*não encontrado*");
    }

    [Fact]
    public async Task Handle_EmailEmUso_DeveLancarConflictException()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(ClienteExistente(id));
        _repoMock.Setup(r => r.ExistsByEmailExceptIdAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _handler.Handle(new UpdateClienteCommand(id, DtoValido()), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*email*");
    }

    [Fact]
    public async Task Handle_DocumentoEmUso_DeveLancarConflictException()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(ClienteExistente(id));
        _repoMock.Setup(r => r.ExistsByEmailExceptIdAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByDocumentoExceptIdAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _handler.Handle(new UpdateClienteCommand(id, DtoValido()), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*documento*");
    }

    [Fact]
    public async Task Handle_NomeInvalido_DeveLancarValidationException()
    {
        var dto = DtoValido();
        dto.Nome = "x";

        var act = () => _handler.Handle(new UpdateClienteCommand(Guid.NewGuid(), dto), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ClienteNaoEncontrado_NaoDeveInvocarSaveChanges()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Cliente?)null);

        var act = () => _handler.Handle(new UpdateClienteCommand(id, DtoValido()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
