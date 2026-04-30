using MediatR;
using Rebus.Clientes.Application.Dtos;

namespace Rebus.Clientes.Application.Features.Clientes.Commands.PublishUpdateCliente;

public record PublishUpdateClienteCommand(Guid Id, ClienteWriteDto Cliente) : IRequest<Guid>;
