using AutoMapper;
using FluentAssertions;
using Moq;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Dtos;
using Rebus.Clientes.Application.Features.Clientes.Queries.GetClientes;
using Rebus.Clientes.Application.Mapping;
using Rebus.Clientes.Domain.Entities;

namespace Rebus.Clientes.UnitTests.Application.Features.Clientes.Queries;

public class GetClientesQueryHandlerTests
{
    private readonly Mock<IClienteRepository> _repoMock = new();
    private readonly IMapper _mapper;
    private readonly GetClientesQueryHandler _handler;

    private const string CpfValido = "52998224725";

    public GetClientesQueryHandlerTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(ApplicationMappingProfile).Assembly));
        _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();
        _handler = new GetClientesQueryHandler(_repoMock.Object, _mapper);
    }

    private static Cliente CriarCliente(string nome, string email, string cpf = CpfValido) =>
        new(Guid.NewGuid(), nome, email, cpf);

    [Fact]
    public async Task Handle_ComClientes_DeveRetornarListaMapeada()
    {
        var clientes = new List<Cliente>
        {
            CriarCliente("João da Silva Santos", "joao@email.com"),
            CriarCliente("Maria Oliveira Souza", "maria@email.com", "11144477735")
        };
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(clientes.AsReadOnly());

        var result = await _handler.Handle(new GetClientesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Email == "joao@email.com");
        result.Should().Contain(c => c.Email == "maria@email.com");
    }

    [Fact]
    public async Task Handle_SemClientes_DeveRetornarListaVazia()
    {
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Cliente>().AsReadOnly());

        var result = await _handler.Handle(new GetClientesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DeveMapearPropriedadesCorretamente()
    {
        var id = Guid.NewGuid();
        var clientes = new List<Cliente> { new(id, "João da Silva Santos", "joao@email.com", CpfValido) };
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(clientes.AsReadOnly());

        var result = await _handler.Handle(new GetClientesQuery(), CancellationToken.None);

        var dto = result.Single();
        dto.Id.Should().Be(id);
        dto.Nome.Should().Be("João da Silva Santos");
        dto.Email.Should().Be("joao@email.com");
        dto.Documento.Should().Be(CpfValido);
    }

    [Fact]
    public async Task Handle_DeveRetornarTipoClienteDto()
    {
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Cliente> { CriarCliente("Nome Completo Aqui", "x@x.com") }.AsReadOnly());

        var result = await _handler.Handle(new GetClientesQuery(), CancellationToken.None);

        result.Should().AllBeOfType<ClienteDto>();
    }
}
