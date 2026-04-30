using MediatR;
using Rebus.Clientes.Application.Dtos;

namespace Rebus.Clientes.Application.Features.Clientes.Commands.CreateCliente;

public record CreateClienteCommand(ClienteWriteDto Cliente) : IRequest<ClienteDto>;
