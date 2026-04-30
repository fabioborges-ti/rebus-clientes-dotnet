using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Moq;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Dtos;
using Rebus.Clientes.Application.Features.Clientes.Commands.CreateCliente;
using Rebus.Clientes.Application.Mapping;
using Rebus.Clientes.Application.Validators;
using Rebus.Clientes.Domain.Entities;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.UnitTests.Application.Features.Clientes.Commands;

public class CreateClienteCommandHandlerTests
{
    private readonly Mock<IClienteRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly IMapper _mapper;
    private readonly IValidator<ClienteWriteDto> _validator;
    private readonly CreateClienteCommandHandler _handler;

    private const string CpfValido = "52998224725";

    public CreateClienteCommandHandlerTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(ApplicationMappingProfile).Assembly));
        _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();
        _validator = new CreateClienteDtoValidator();
        _handler = new CreateClienteCommandHandler(_repoMock.Object, _uowMock.Object, _mapper, _validator);
    }

    private ClienteWriteDto DtoValido() => new()
    {
        Nome = "João da Silva Santos",
        Email = "joao@email.com",
        Documento = CpfValido
    };

    private void ConfigurarRepoSemConflitos()
    {
        _repoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByDocumentoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task Handle_DadosValidos_DevePersistirERetornarClienteDto()
    {
        ConfigurarRepoSemConflitos();

        var result = await _handler.Handle(new CreateClienteCommand(DtoValido()), CancellationToken.None);

        result.Should().NotBeNull();
        result.Nome.Should().Be("João da Silva Santos");
        result.Email.Should().Be("joao@email.com");
        result.Documento.Should().Be(CpfValido);
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_DadosValidos_DeveInvocarAddESaveChanges()
    {
        ConfigurarRepoSemConflitos();

        await _handler.Handle(new CreateClienteCommand(DtoValido()), CancellationToken.None);

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailJaCadastrado_DeveLancarConflictException()
    {
        _repoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _handler.Handle(new CreateClienteCommand(DtoValido()), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*email*");
    }

    [Fact]
    public async Task Handle_DocumentoJaCadastrado_DeveLancarConflictException()
    {
        _repoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByDocumentoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _handler.Handle(new CreateClienteCommand(DtoValido()), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*documento*");
    }

    [Fact]
    public async Task Handle_NomeInvalido_DeveLancarValidationException()
    {
        var dto = DtoValido();
        dto.Nome = "Curto";

        var act = () => _handler.Handle(new CreateClienteCommand(dto), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailInvalido_DeveLancarValidationException()
    {
        var dto = DtoValido();
        dto.Email = "email-invalido";

        var act = () => _handler.Handle(new CreateClienteCommand(dto), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_CpfInvalido_DeveLancarValidationException()
    {
        var dto = DtoValido();
        dto.Documento = "00000000000";

        var act = () => _handler.Handle(new CreateClienteCommand(dto), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_EmailComMaiusculas_DevePersistirNormalizado()
    {
        _repoMock.Setup(r => r.ExistsByEmailAsync("joao@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByDocumentoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var dto = DtoValido();
        dto.Email = "JOAO@EMAIL.COM";

        var result = await _handler.Handle(new CreateClienteCommand(dto), CancellationToken.None);

        result.Email.Should().Be("joao@email.com");
    }

    [Fact]
    public async Task Handle_ConflitoPorEmail_NaoDeveInvocarSaveChanges()
    {
        _repoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _handler.Handle(new CreateClienteCommand(DtoValido()), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
