using MediatR;

namespace Rebus.Clientes.Application.Features.Clientes.Commands.DeleteCliente;

public record DeleteClienteCommand(Guid Id) : IRequest;
