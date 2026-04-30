using AutoMapper;
using MediatR;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Dtos;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.Application.Features.Clientes.Queries.GetClienteById;

public class GetClienteByIdQueryHandler : IRequestHandler<GetClienteByIdQuery, ClienteDto>
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IMapper _mapper;

    public GetClienteByIdQueryHandler(IClienteRepository clienteRepository, IMapper mapper)
    {
        _clienteRepository = clienteRepository;
        _mapper = mapper;
    }

    public async Task<ClienteDto> Handle(GetClienteByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _clienteRepository.GetByIdAsync(request.Id, cancellationToken);

        if (entity is null)
            throw new NotFoundException("Cliente não encontrado.");

        return _mapper.Map<ClienteDto>(entity);
    }
}
