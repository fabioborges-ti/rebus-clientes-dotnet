using MediatR;
using Rebus.Clientes.Application.Dtos;

namespace Rebus.Clientes.Application.Features.Clientes.Queries.GetClientesPaged;

/// <param name="Page">Número da página solicitada (mínimo: 1).</param>
/// <param name="PageSize">Quantidade de registros por página (mínimo: 1, máximo: 100).</param>
public record GetClientesPagedQuery(int Page, int PageSize)
    : IRequest<PagedResultDto<ClienteDto>>;
