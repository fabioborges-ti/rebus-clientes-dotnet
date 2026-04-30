using FluentAssertions;
using Moq;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Features.Clientes.Commands.DeleteCliente;
using Rebus.Clientes.Domain.Entities;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.UnitTests.Application.Features.Clientes.Commands;

public class DeleteClienteCommandHandlerTests
{
    private readonly Mock<IClienteRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DeleteClienteCommandHandler _handler;

    private const string CpfValido = "52998224725";

    public DeleteClienteCommandHandlerTests()
    {
        _handler = new DeleteClienteCommandHandler(_repoMock.Object, _uowMock.Object);
    }

    private static Cliente ClienteExistente() =>
        new(Guid.NewGuid(), "Nome Completo Teste", "teste@email.com", CpfValido);

    [Fact]
    public async Task Handle_ClienteExistente_DeveRemoverESalvar()
    {
        var id = Guid.NewGuid();
        var entity = ClienteExistente();
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        await _handler.Handle(new DeleteClienteCommand(id), CancellationToken.None);

        _repoMock.Verify(r => r.Remove(entity), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ClienteNaoEncontrado_DeveLancarNotFoundException()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Cliente?)null);

        var act = () => _handler.Handle(new DeleteClienteCommand(id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*não encontrado*");
    }

    [Fact]
    public async Task Handle_ClienteNaoEncontrado_NaoDeveInvocarRemoveNemSaveChanges()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Cliente?)null);

        var act = () => _handler.Handle(new DeleteClienteCommand(id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repoMock.Verify(r => r.Remove(It.IsAny<Cliente>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DevePassarOIdCorretoParaBusca()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(ClienteExistente());

        await _handler.Handle(new DeleteClienteCommand(id), CancellationToken.None);

        _repoMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
