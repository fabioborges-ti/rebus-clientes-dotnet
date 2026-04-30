using MediatR;
using Rebus.Clientes.Application.Dtos;

namespace Rebus.Clientes.Application.Features.Clientes.Commands.UpdateCliente;

public record UpdateClienteCommand(Guid Id, ClienteWriteDto Cliente) : IRequest<ClienteDto?>;
