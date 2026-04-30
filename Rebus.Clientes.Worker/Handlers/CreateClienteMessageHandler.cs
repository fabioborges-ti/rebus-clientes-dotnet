using MediatR;
using Rebus.Handlers;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Dtos;
using Rebus.Clientes.Application.Features.Clientes.Commands.CreateCliente;
using Rebus.Clientes.Application.Messaging;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.Worker.Handlers;

/// <summary>
/// Handler Rebus responsável por processar mensagens de criação de cliente vindas da fila.
///
/// Este componente vive no Worker e representa o lado CONSUMIDOR do fluxo assíncrono:
///   RabbitMQ → [este handler] → CreateClienteCommand (MediatR) → PostgreSQL
///
/// O Rebus invoca Handle() automaticamente para cada mensagem dequeued de "clientes-commands-queue".
/// O handler é descoberto e registrado via AutoRegisterHandlersFromAssemblyOf no Program.cs do Worker —
/// não é necessário registrá-lo manualmente no DI container.
///
/// Rastreamento de operação:
///   - CorrelationId (da mensagem) identifica a ClienteOperacao registrada com estado Pendente pela API
///   - Sucesso → MarcarConcluida() atualiza para Concluída e associa o ClienteId gerado
///   - Falha de negócio → MarcarFalha() atualiza para Falhou e re-throw para o Rebus
///   - Re-throw sinaliza ao Rebus que houve falha: após N tentativas, a mensagem vai para a dead-letter queue
/// </summary>
public class CreateClienteMessageHandler : IHandleMessages<CreateClienteMessage>
{
    private readonly IMediator _mediator;
    private readonly IClienteOperacaoRepository _operacaoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateClienteMessageHandler> _logger;

    public CreateClienteMessageHandler(
        IMediator mediator,
        IClienteOperacaoRepository operacaoRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateClienteMessageHandler> logger)
    {
        _mediator = mediator;
        _operacaoRepository = operacaoRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(CreateClienteMessage message)
    {
        _logger.LogInformation("Processando criação de cliente. CorrelationId: {CorrelationId}", message.CorrelationId);

        // Reconstrói o DTO a partir dos dados da mensagem.
        // O DTO é o contrato da camada Application; a mensagem é o contrato da mensageria.
        // Essa separação evita que mudanças no protocolo de fila afetem a lógica de negócio.
        var dto = new ClienteWriteDto
        {
            Nome = message.Nome,
            Email = message.Email,
            Documento = message.Documento
        };

        try
        {
            // Delega a criação ao mesmo handler MediatR usado no fluxo síncrono.
            // Reutilizar CreateClienteCommand garante que as mesmas regras de negócio
            // (validação, persistência) se apliquem em ambos os caminhos.
            var result = await _mediator.Send(new CreateClienteCommand(dto));

            // Atualiza o rastreamento para que o cliente possa consultar o status via CorrelationId.
            // É aqui que a operação sai do estado Pendente para Concluída.
            await MarcarOperacaoConcluidaAsync(message.CorrelationId, result.Id);
            _logger.LogInformation("Cliente persistido com sucesso. CorrelationId: {CorrelationId}", message.CorrelationId);
        }
        catch (ConflictException ex)
        {
            // Race condition: outro processo criou um cliente com mesmo e-mail/documento
            // entre a validação antecipada da API e o processamento aqui no Worker.
            // Marcamos a operação como falha para que o cliente saiba o motivo via polling.
            await MarcarOperacaoFalhaAsync(message.CorrelationId, ex.Message);
            _logger.LogWarning(ex, "Conflito de negócio ao criar cliente. CorrelationId: {CorrelationId}", message.CorrelationId);

            // Re-throw obrigatório: sinaliza ao Rebus que a mensagem não foi processada com sucesso.
            // O Rebus decidirá entre retry ou envio para dead-letter queue conforme a configuração.
            throw;
        }
    }

    private async Task MarcarOperacaoConcluidaAsync(Guid correlationId, Guid clienteId)
    {
        var op = await _operacaoRepository.GetByCorrelationIdAsync(correlationId, CancellationToken.None);
        if (op is null)
            return;

        op.MarcarConcluida(clienteId);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    private async Task MarcarOperacaoFalhaAsync(Guid correlationId, string mensagem)
    {
        var op = await _operacaoRepository.GetByCorrelationIdAsync(correlationId, CancellationToken.None);
        if (op is null)
            return;

        op.MarcarFalha(mensagem);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
    }
}
