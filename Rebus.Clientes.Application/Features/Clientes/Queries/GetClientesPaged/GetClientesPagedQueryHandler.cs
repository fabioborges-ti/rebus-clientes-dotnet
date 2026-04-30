using AutoMapper;
using MediatR;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Dtos;

namespace Rebus.Clientes.Application.Features.Clientes.Queries.GetClientesPaged;

public class GetClientesPagedQueryHandler
    : IRequestHandler<GetClientesPagedQuery, PagedResultDto<ClienteDto>>
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IMapper _mapper;

    public GetClientesPagedQueryHandler(IClienteRepository clienteRepository, IMapper mapper)
    {
        _clienteRepository = clienteRepository;
        _mapper = mapper;
    }

    public async Task<PagedResultDto<ClienteDto>> Handle(
        GetClientesPagedQuery request,
        CancellationToken cancellationToken)
    {
        var (items, total) = await _clienteRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            cancellationToken);

        return new PagedResultDto<ClienteDto>
        {
            Items = items.Select(_mapper.Map<ClienteDto>).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRegistros = total
        };
    }
}
