using MediatR;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.Application.Features.Clientes.Commands.DeleteCliente;

public class DeleteClienteCommandHandler : IRequestHandler<DeleteClienteCommand>
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteClienteCommandHandler(IClienteRepository clienteRepository, IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteClienteCommand request, CancellationToken cancellationToken)
    {
        var entity = await _clienteRepository.GetByIdAsync(request.Id, cancellationToken);

        if (entity is null)
            throw new NotFoundException("Cliente não encontrado.");

        _clienteRepository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
