using MediatR;
using Rebus.Clientes.Application.Dtos;

namespace Rebus.Clientes.Application.Features.Clientes.Queries.GetClientes;

public record GetClientesQuery() : IRequest<IReadOnlyList<ClienteDto>>;
