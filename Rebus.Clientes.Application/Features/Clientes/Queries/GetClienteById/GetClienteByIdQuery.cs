using MediatR;
using Rebus.Clientes.Application.Dtos;

namespace Rebus.Clientes.Application.Features.Clientes.Queries.GetClienteById;

public record GetClienteByIdQuery(Guid Id) : IRequest<ClienteDto>;
