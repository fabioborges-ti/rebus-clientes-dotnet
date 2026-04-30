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

/// <summary>
/// Handler MediatR responsável pela etapa de "aceite" do fluxo assíncrono de atualização de clientes.
///
/// FLUXO COMPLETO — o que acontece quando PUT /clientes/{id} é chamado:
///   [1] API recebe a requisição e despacha PublishUpdateClienteCommand via MediatR
///   [2] → Este handler valida dados, verifica existência e unicidade antecipadamente  (falha rápida)
///   [3] → Persiste ClienteOperacao com estado Pendente                                (rastreamento)
///   [4] → Publica UpdateClienteMessage no RabbitMQ                                   (enfileiramento)
///   [5] → Retorna CorrelationId ao cliente com HTTP 202 Accepted                     (resposta imediata)
///   [6] → Worker consome a mensagem e atualiza o Cliente no banco                    (processamento real)
///   [7] → Cliente consulta GET /operacoes/{correlationId} para ver o status          (polling)
///
/// Difere do PublishCreateCliente por incluir verificação de existência do cliente (NotFoundException)
/// e verificações de unicidade que excluem o próprio cliente sendo atualizado (ExceptId).
/// </summary>
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
        // PASSO [2] — Validação antecipada (fail fast)
        // Mesma estratégia do Create: rejeitar na API antes de qualquer custo de fila.
        await _validator.ValidateAndThrowAsync(request.Cliente, cancellationToken);

        // PASSO [2b] — Verificação de existência antecipada
        // Se o cliente não existe, retornamos 404 imediatamente em vez de enfileirar
        // uma mensagem que o Worker também rejeitaria (com muito mais latência).
        var clienteExiste = await _clienteRepository.GetByIdAsync(request.Id, cancellationToken);
        if (clienteExiste is null)
        {
            _logger.LogWarning("Atualização rejeitada: cliente não encontrado. ClienteId: {ClienteId}", request.Id);
            throw new NotFoundException("Cliente não encontrado.");
        }

        var emailNormalizado = request.Cliente.Email.Trim().ToLowerInvariant();

        // PASSO [2c] — Verificação de unicidade de e-mail excluindo o próprio cliente
        // ExistsByEmailExceptIdAsync garante que não haverá falso conflito ao manter o mesmo e-mail.
        var emailEmUso = await _clienteRepository.ExistsByEmailExceptIdAsync(request.Id, emailNormalizado, cancellationToken);

        if (emailEmUso)
        {
            _logger.LogWarning("Atualização rejeitada: e-mail já pertence a outro cliente. ClienteId: {ClienteId}", request.Id);
            throw new ConflictException("E-mail já pertence a outro cliente.");
        }

        var documentoNormalizado = request.Cliente.Documento.Trim();

        // PASSO [2d] — Verificação de unicidade de documento excluindo o próprio cliente
        var documentoEmUso = await _clienteRepository.ExistsByDocumentoExceptIdAsync(request.Id, documentoNormalizado, cancellationToken);

        if (documentoEmUso)
        {
            _logger.LogWarning("Atualização rejeitada: documento já pertence a outro cliente. ClienteId: {ClienteId}", request.Id);
            throw new ConflictException("Documento já pertence a outro cliente.");
        }

        // Monta o contrato da mensagem incluindo o ClienteId para o Worker saber qual registro atualizar.
        var message = new UpdateClienteMessage
        {
            CorrelationId = Guid.NewGuid(),
            ClienteId = request.Id,
            Nome = request.Cliente.Nome.Trim(),
            Email = emailNormalizado,
            Documento = documentoNormalizado,
            EnfileiradoEmUtc = DateTime.UtcNow
        };

        // PASSO [3] — Persistência do rastreamento ANTES de enfileirar
        // Registra a operação como Pendente antes de publicar para garantir que nenhuma
        // operação fique "órfã" sem registro caso a aplicação falhe após a publicação.
        var operacao = new ClienteOperacao(message.CorrelationId, OperacaoTipo.Atualizacao, request.Id);
        await _operacaoRepository.AddAsync(operacao, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // PASSO [4] — Publicação na fila (Fire & Forget)
        // A partir daqui o Worker assume o processamento. A API retorna imediatamente.
        await _messageBus.PublishAsync(message, cancellationToken);

        _logger.LogInformation(
            "Solicitação de atualização enfileirada. CorrelationId: {CorrelationId} ClienteId: {ClienteId}",
            message.CorrelationId,
            request.Id);

        return message.CorrelationId;
    }
}
