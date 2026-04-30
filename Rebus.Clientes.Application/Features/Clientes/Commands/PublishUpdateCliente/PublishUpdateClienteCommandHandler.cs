using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Rebus.Clientes.Application.Abstractions.Messaging;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Messaging;
using Rebus.Clientes.Domain.Entities;
using Rebus.Clientes.Domain.Enums;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.Application.Features.Clientes.Commands.PublishUpdateCliente;

public class PublishUpdateClienteCommandHandler : IRequestHandler<PublishUpdateClienteCommand, Guid>
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IClienteOperacaoRepository _operacaoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClienteMessageBus _messageBus;
    private readonly IValidator<Dtos.ClienteWriteDto> _validator;
    private readonly ILogger<PublishUpdateClienteCommandHandler> _logger;

    public PublishUpdateClienteCommandHandler(
        IClienteRepository clienteRepository,
        IClienteOperacaoRepository operacaoRepository,
        IUnitOfWork unitOfWork,
        IClienteMessageBus messageBus,
        IValidator<Dtos.ClienteWriteDto> validator,
        ILogger<PublishUpdateClienteCommandHandler> logger)
    {
        _clienteRepository = clienteRepository;
        _operacaoRepository = operacaoRepository;
        _unitOfWork = unitOfWork;
        _messageBus = messageBus;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Guid> Handle(PublishUpdateClienteCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request.Cliente, cancellationToken);

        var clienteExiste = await _clienteRepository.GetByIdAsync(request.Id, cancellationToken);
        if (clienteExiste is null)
        {
            _logger.LogWarning("Atualização rejeitada: cliente não encontrado. ClienteId: {ClienteId}", request.Id);
            throw new NotFoundException("Cliente não encontrado.");
        }

        var emailNormalizado = request.Cliente.Email.Trim().ToLowerInvariant();
        var emailEmUso = await _clienteRepository.ExistsByEmailExceptIdAsync(request.Id, emailNormalizado, cancellationToken);

        if (emailEmUso)
        {
            _logger.LogWarning("Atualização rejeitada: e-mail já pertence a outro cliente. ClienteId: {ClienteId}", request.Id);
            throw new ConflictException("E-mail já pertence a outro cliente.");
        }

        var documentoNormalizado = request.Cliente.Documento.Trim();
        var documentoEmUso = await _clienteRepository.ExistsByDocumentoExceptIdAsync(request.Id, documentoNormalizado, cancellationToken);

        if (documentoEmUso)
        {
            _logger.LogWarning("Atualização rejeitada: documento já pertence a outro cliente. ClienteId: {ClienteId}", request.Id);
            throw new ConflictException("Documento já pertence a outro cliente.");
        }

        var message = new UpdateClienteMessage
        {
            CorrelationId = Guid.NewGuid(),
            ClienteId = request.Id,
            Nome = request.Cliente.Nome.Trim(),
            Email = emailNormalizado,
            Documento = documentoNormalizado,
            EnfileiradoEmUtc = DateTime.UtcNow
        };

        var operacao = new ClienteOperacao(message.CorrelationId, OperacaoTipo.Atualizacao, request.Id);
        await _operacaoRepository.AddAsync(operacao, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _messageBus.PublishAsync(message, cancellationToken);

        _logger.LogInformation(
            "Solicitação de atualização enfileirada. CorrelationId: {CorrelationId} ClienteId: {ClienteId}",
            message.CorrelationId,
            request.Id);

        return message.CorrelationId;
    }
}
