using AutoMapper;
using FluentValidation;
using MediatR;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Dtos;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.Application.Features.Clientes.Commands.UpdateCliente;

public class UpdateClienteCommandHandler : IRequestHandler<UpdateClienteCommand, ClienteDto?>
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<ClienteWriteDto> _validator;

    public UpdateClienteCommandHandler(
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

    public async Task<ClienteDto?> Handle(UpdateClienteCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request.Cliente, cancellationToken);

        var entity = await _clienteRepository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException("Cliente não encontrado.");
        }

        if (await _clienteRepository.ExistsByEmailExceptIdAsync(request.Id, request.Cliente.Email.Trim().ToLowerInvariant(), cancellationToken))
        {
            throw new ConflictException("Já existe cliente com este email.");
        }

        if (await _clienteRepository.ExistsByDocumentoExceptIdAsync(request.Id, request.Cliente.Documento.Trim(), cancellationToken))
        {
            throw new ConflictException("Já existe cliente com este documento.");
        }

        entity.Atualizar(request.Cliente.Nome, request.Cliente.Email, request.Cliente.Documento);
        _clienteRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ClienteDto>(entity);
    }
}
