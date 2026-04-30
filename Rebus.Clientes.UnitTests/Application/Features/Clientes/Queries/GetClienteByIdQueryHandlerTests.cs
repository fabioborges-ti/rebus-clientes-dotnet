using AutoMapper;
using FluentAssertions;
using Moq;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Features.Clientes.Queries.GetClienteById;
using Rebus.Clientes.Application.Mapping;
using Rebus.Clientes.Domain.Entities;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.UnitTests.Application.Features.Clientes.Queries;

public class GetClienteByIdQueryHandlerTests
{
    private readonly Mock<IClienteRepository> _repoMock = new();
    private readonly IMapper _mapper;
    private readonly GetClienteByIdQueryHandler _handler;

    private const string CpfValido = "52998224725";

    public GetClienteByIdQueryHandlerTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(ApplicationMappingProfile).Assembly));
        _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();
        _handler = new GetClienteByIdQueryHandler(_repoMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_ClienteExistente_DeveRetornarClienteDto()
    {
        var id = Guid.NewGuid();
        var entity = new Cliente(id, "João da Silva Santos", "joao@email.com", CpfValido);
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await _handler.Handle(new GetClienteByIdQuery(id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(id);
        result.Nome.Should().Be("João da Silva Santos");
        result.Email.Should().Be("joao@email.com");
        result.Documento.Should().Be(CpfValido);
    }

    [Fact]
    public async Task Handle_ClienteNaoEncontrado_DeveLancarNotFoundException()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Cliente?)null);

        var act = () => _handler.Handle(new GetClienteByIdQuery(id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*não encontrado*");
    }

    [Fact]
    public async Task Handle_DevePassarIdCorretoPararBusca()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Cliente(id, "Nome Completo Aqui", "a@b.com", CpfValido));

        await _handler.Handle(new GetClienteByIdQuery(id), CancellationToken.None);

        _repoMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
