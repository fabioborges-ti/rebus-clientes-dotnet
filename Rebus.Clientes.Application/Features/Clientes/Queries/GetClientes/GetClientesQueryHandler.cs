using AutoMapper;
using MediatR;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Dtos;

namespace Rebus.Clientes.Application.Features.Clientes.Queries.GetClientes;

public class GetClientesQueryHandler : IRequestHandler<GetClientesQuery, IReadOnlyList<ClienteDto>>
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IMapper _mapper;

    public GetClientesQueryHandler(IClienteRepository clienteRepository, IMapper mapper)
    {
        _clienteRepository = clienteRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ClienteDto>> Handle(GetClientesQuery request, CancellationToken cancellationToken)
    {
        var entities = await _clienteRepository.GetAllAsync(cancellationToken);
        return entities.Select(_mapper.Map<ClienteDto>).ToList();
    }
}
