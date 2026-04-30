using MediatR;
using Rebus.Clientes.Application.Dtos;

namespace Rebus.Clientes.Application.Features.Clientes.Commands.PublishCreateCliente;

public record PublishCreateClienteCommand(ClienteWriteDto Cliente) : IRequest<Guid>;
