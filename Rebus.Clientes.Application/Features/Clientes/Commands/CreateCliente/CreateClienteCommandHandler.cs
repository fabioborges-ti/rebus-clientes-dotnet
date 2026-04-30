using AutoMapper;
using FluentValidation;
using MediatR;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Dtos;
using Rebus.Clientes.Domain.Entities;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.Application.Features.Clientes.Commands.CreateCliente;

public class CreateClienteCommandHandler : IRequestHandler<CreateClienteCommand, ClienteDto>
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<ClienteWriteDto> _validator;

    public CreateClienteCommandHandler(
        IClienteRepository clienteRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IValidator<ClienteWriteDto> validator)
    {
        _clienteRepository = clienteRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<ClienteDto> Handle(CreateClienteCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request.Cliente, cancellationToken);
        await ValidateBusinessRulesAsync(request.Cliente, cancellationToken);

        var entity = new Cliente(Guid.NewGuid(), request.Cliente.Nome, request.Cliente.Email, request.Cliente.Documento);
        await _clienteRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ClienteDto>(entity);
    }

    private async Task ValidateBusinessRulesAsync(ClienteWriteDto dto, CancellationToken cancellationToken)
    {
        if (await _clienteRepository.ExistsByEmailAsync(dto.Email.Trim().ToLowerInvariant(), cancellationToken))
        {
            throw new ConflictException("Já existe cliente com este email.");
        }

        if (await _clienteRepository.ExistsByDocumentoAsync(dto.Documento.Trim(), cancellationToken))
        {
            throw new ConflictException("Já existe cliente com este documento.");
        }
    }
}
