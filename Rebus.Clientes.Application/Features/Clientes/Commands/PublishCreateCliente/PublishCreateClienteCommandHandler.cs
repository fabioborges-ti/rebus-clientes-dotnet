using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Rebus.Clientes.Application.Abstractions.Messaging;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Messaging;
using Rebus.Clientes.Domain.Entities;
using Rebus.Clientes.Domain.Enums;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.Application.Features.Clientes.Commands.PublishCreateCliente;

public class PublishCreateClienteCommandHandler : IRequestHandler<PublishCreateClienteCommand, Guid>
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IClienteOperacaoRepository _operacaoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClienteMessageBus _messageBus;
    private readonly IValidator<Dtos.ClienteWriteDto> _validator;
    private readonly ILogger<PublishCreateClienteCommandHandler> _logger;

    public PublishCreateClienteCommandHandler(
        IClienteRepository clienteRepository,
        IClienteOperacaoRepository operacaoRepository,
        IUnitOfWork unitOfWork,
        IClienteMessageBus messageBus,
        IValidator<Dtos.ClienteWriteDto> validator,
        ILogger<PublishCreateClienteCommandHandler> logger)
    {
        _clienteRepository = clienteRepository;
        _operacaoRepository = operacaoRepository;
        _unitOfWork = unitOfWork;
        _messageBus = messageBus;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Guid> Handle(PublishCreateClienteCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request.Cliente, cancellationToken);

        var emailNormalizado = request.Cliente.Email.Trim().ToLowerInvariant();
        var emailJaCadastrado = await _clienteRepository.ExistsByEmailAsync(emailNormalizado, cancellationToken);

        if (emailJaCadastrado)
        {
            _logger.LogWarning("Criação rejeitada: e-mail já cadastrado. Email: {Email}", emailNormalizado);
            throw new ConflictException("E-mail já cadastrado.");
        }

        var documentoNormalizado = request.Cliente.Documento.Trim();
        var documentoJaCadastrado = await _clienteRepository.ExistsByDocumentoAsync(documentoNormalizado, cancellationToken);

        if (documentoJaCadastrado)
        {
            _logger.LogWarning("Criação rejeitada: documento já cadastrado. Documento: {Documento}", documentoNormalizado);
            throw new ConflictException("Documento já cadastrado.");
        }

        var message = new CreateClienteMessage
        {
            CorrelationId = Guid.NewGuid(),
            Nome = request.Cliente.Nome.Trim(),
            Email = emailNormalizado,
            Documento = documentoNormalizado,
            EnfileiradoEmUtc = DateTime.UtcNow
        };

        var operacao = new ClienteOperacao(message.CorrelationId, OperacaoTipo.Criacao);
        await _operacaoRepository.AddAsync(operacao, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _messageBus.PublishAsync(message, cancellationToken);

        _logger.LogInformation("Solicitação de criação enfileirada. CorrelationId: {CorrelationId}", message.CorrelationId);

        return message.CorrelationId;
    }
}
