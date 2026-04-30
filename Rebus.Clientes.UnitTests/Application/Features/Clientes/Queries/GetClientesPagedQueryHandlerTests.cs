using AutoMapper;
using FluentAssertions;
using Moq;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Features.Clientes.Queries.GetClientesPaged;
using Rebus.Clientes.Application.Mapping;
using Rebus.Clientes.Domain.Entities;

namespace Rebus.Clientes.UnitTests.Application.Features.Clientes.Queries;

public class GetClientesPagedQueryHandlerTests
{
    private readonly Mock<IClienteRepository> _repoMock = new();
    private readonly IMapper _mapper;
    private readonly GetClientesPagedQueryHandler _handler;

    private const string CpfValido = "52998224725";

    public GetClientesPagedQueryHandlerTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(ApplicationMappingProfile).Assembly));
        _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();
        _handler = new GetClientesPagedQueryHandler(_repoMock.Object, _mapper);
    }

    private static Cliente CriarCliente(string email) =>
        new(Guid.NewGuid(), "Nome Completo Qualquer", email, CpfValido);

    [Fact]
    public async Task Handle_DeveRetornarItensDaPaginaCorreta()
    {
        var itens = new List<Cliente>
        {
            CriarCliente("a@a.com"),
            CriarCliente("b@b.com")
        };
        _repoMock.Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((itens.AsReadOnly() as IReadOnlyList<Cliente>, 25));

        var result = await _handler.Handle(new GetClientesPagedQuery(1, 10), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalRegistros.Should().Be(25);
    }

    [Fact]
    public async Task Handle_DeveCalcularTotalPaginasCorretamente()
    {
        _repoMock.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Cliente>().AsReadOnly() as IReadOnlyList<Cliente>, 25));

        var result = await _handler.Handle(new GetClientesPagedQuery(1, 10), CancellationToken.None);

        result.TotalPaginas.Should().Be(3); // ceil(25/10)
    }

    [Fact]
    public async Task Handle_TotalDivisivel_DeveCalcularTotalPaginasExato()
    {
        _repoMock.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Cliente>().AsReadOnly() as IReadOnlyList<Cliente>, 20));

        var result = await _handler.Handle(new GetClientesPagedQuery(1, 10), CancellationToken.None);

        result.TotalPaginas.Should().Be(2); // ceil(20/10)
    }

    [Fact]
    public async Task Handle_SemRegistros_DeveRetornarListaVaziaEZeroTotais()
    {
        _repoMock.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Cliente>().AsReadOnly() as IReadOnlyList<Cliente>, 0));

        var result = await _handler.Handle(new GetClientesPagedQuery(1, 10), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalRegistros.Should().Be(0);
        result.TotalPaginas.Should().Be(0);
    }

    [Fact]
    public async Task Handle_DevePassarPaginacaoCorretaParaRepositorio()
    {
        _repoMock.Setup(r => r.GetPagedAsync(3, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Cliente>().AsReadOnly() as IReadOnlyList<Cliente>, 0));

        await _handler.Handle(new GetClientesPagedQuery(3, 5), CancellationToken.None);

        _repoMock.Verify(r => r.GetPagedAsync(3, 5, It.IsAny<CancellationToken>()), Times.Once);
    }
}
