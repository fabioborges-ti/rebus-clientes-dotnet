using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Rebus.Clientes.Application.Abstractions.Correlation;
using Rebus.Clientes.Application.Abstractions.Messaging;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Messaging;
using Rebus.Clientes.Domain.Entities;
using Rebus.Clientes.Domain.Enums;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.Application.Features.Clientes.Commands.PublishCreateCliente;

/// <summary>
/// Handler MediatR responsável pela etapa de "aceite" do fluxo assíncrono de criação de clientes.
///
/// FLUXO COMPLETO — o que acontece quando POST /clientes é chamado:
///   [1] API recebe a requisição e despacha PublishCreateClienteCommand via MediatR
///   [2] → Este handler valida dados e regras de unicidade antecipadamente   (falha rápida)
///   [3] → Persiste ClienteOperacao com estado Pendente                       (rastreamento)
///   [4] → Publica CreateClienteMessage no RabbitMQ                           (enfileiramento)
///   [5] → Retorna CorrelationId ao cliente com HTTP 202 Accepted             (resposta imediata)
///   [6] → Worker consome a mensagem e persiste o Cliente no banco             (processamento real)
///   [7] → Cliente consulta GET /operacoes/{correlationId} para ver o status  (polling)
///
/// Este handler executa os passos [2] a [5]. O cliente recebe resposta imediata
/// sem aguardar a persistência real — padrão Fire &amp; Forget com rastreamento por CorrelationId.
/// </summary>
public class PublishCreateClienteCommandHandler : IRequestHandler<PublishCreateClienteCommand, Guid>
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IClienteOperacaoRepository _operacaoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClienteMessageBus _messageBus;
    private readonly IValidator<Dtos.ClienteWriteDto> _validator;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly ILogger<PublishCreateClienteCommandHandler> _logger;

    public PublishCreateClienteCommandHandler(
        IClienteRepository clienteRepository,
        IClienteOperacaoRepository operacaoRepository,
        IUnitOfWork unitOfWork,
        IClienteMessageBus messageBus,
        IValidator<Dtos.ClienteWriteDto> validator,
        ICorrelationIdAccessor correlationIdAccessor,
        ILogger<PublishCreateClienteCommandHandler> logger)
    {
        _clienteRepository = clienteRepository;
        _operacaoRepository = operacaoRepository;
        _unitOfWork = unitOfWork;
        _messageBus = messageBus;
        _validator = validator;
        _correlationIdAccessor = correlationIdAccessor;
        _logger = logger;
    }

    public async Task<Guid> Handle(PublishCreateClienteCommand request, CancellationToken cancellationToken)
    {
        // PASSO [2] — Validação antecipada (fail fast)
        // Rejeitamos aqui, na API, antes de qualquer custo de fila ou banco.
        // FluentValidation lança ValidationException se as regras falharem (formato, tamanho etc.).
        await _validator.ValidateAndThrowAsync(request.Cliente, cancellationToken);

        var emailNormalizado = request.Cliente.Email.Trim().ToLowerInvariant();

        // PASSO [2b] — Verificação de unicidade de e-mail antecipada
        // Embora o Worker também execute validações, verificar aqui evita enfileirar
        // uma mensagem que sabemos que vai falhar — melhora a experiência do cliente
        // com resposta imediata (409 Conflict) em vez de aguardar o processamento assíncrono.
        var emailJaCadastrado = await _clienteRepository.ExistsByEmailAsync(emailNormalizado, cancellationToken);

        if (emailJaCadastrado)
        {
            _logger.LogWarning("Criação rejeitada: e-mail já cadastrado. Email: {Email}", emailNormalizado);
            throw new ConflictException("E-mail já cadastrado.");
        }

        var documentoNormalizado = request.Cliente.Documento.Trim();

        // PASSO [2c] — Verificação de unicidade de documento antecipada (mesma razão do e-mail)
        var documentoJaCadastrado = await _clienteRepository.ExistsByDocumentoAsync(documentoNormalizado, cancellationToken);

        if (documentoJaCadastrado)
        {
            _logger.LogWarning("Criação rejeitada: documento já cadastrado. Documento: {Documento}", documentoNormalizado);
            throw new ConflictException("Documento já cadastrado.");
        }

        // Monta o contrato da mensagem que será enviado pelo RabbitMQ.
        // O CorrelationId é gerado aqui e serve como "ticket" para o cliente rastrear a operação.
        var correlationId = _correlationIdAccessor.GetCorrelationId();
        var message = new CreateClienteMessage
        {
            CorrelationId = correlationId,
            Nome = request.Cliente.Nome.Trim(),
            Email = emailNormalizado,
            Documento = documentoNormalizado,
            EnfileiradoEmUtc = DateTime.UtcNow
        };

        // PASSO [3] — Persistência do rastreamento ANTES de enfileirar
        // Registramos a operação como Pendente antes de publicar a mensagem.
        // Se a aplicação cair após publicar mas antes de persistir, perderíamos o rastreamento.
        // A ordem correta é: salvar no banco → publicar na fila (e não o contrário).
        var operacao = new ClienteOperacao(correlationId, OperacaoTipo.Criacao);
        await _operacaoRepository.AddAsync(operacao, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // PASSO [4] — Publicação na fila (Fire & Forget)
        // A mensagem é enviada ao RabbitMQ via Rebus. A partir daqui, o Worker é responsável
        // pelo processamento. A API retorna imediatamente após este passo.
        await _messageBus.PublishAsync(message, cancellationToken);

        _logger.LogInformation("Solicitação de criação enfileirada. CorrelationId: {CorrelationId}", correlationId);

        return correlationId;
    }
}
