using MediatR;
using Rebus.Handlers;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Dtos;
using Rebus.Clientes.Application.Features.Clientes.Commands.UpdateCliente;
using Rebus.Clientes.Application.Messaging;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.Worker.Handlers;

/// <summary>
/// Handler Rebus responsável por processar mensagens de atualização de cliente vindas da fila.
///
/// Este componente vive no Worker e representa o lado CONSUMIDOR do fluxo assíncrono de atualização:
///   RabbitMQ → [este handler] → UpdateClienteCommand (MediatR) → PostgreSQL
///
/// Dois tipos de falha de negócio são tratados explicitamente:
///   - <see cref="NotFoundException"/>: cliente foi deletado enquanto a mensagem aguardava na fila
///     (race condition entre o DELETE síncrono da API e o processamento assíncrono no Worker)
///   - <see cref="ConflictException"/>: outro cliente assumiu o e-mail/documento entre a validação
///     antecipada da API e o processamento aqui (race condition de unicidade)
///
/// Ambos marcam a operação como Falhou e fazem re-throw para que o Rebus possa gerenciar
/// a política de retentativas e eventual envio para dead-letter queue.
/// </summary>
public class UpdateClienteMessageHandler : IHandleMessages<UpdateClienteMessage>
{
    private readonly IMediator _mediator;
    private readonly IClienteOperacaoRepository _operacaoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateClienteMessageHandler> _logger;

    public UpdateClienteMessageHandler(
        IMediator mediator,
        IClienteOperacaoRepository operacaoRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateClienteMessageHandler> logger)
    {
        _mediator = mediator;
        _operacaoRepository = operacaoRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(UpdateClienteMessage message)
    {
        _logger.LogInformation(
            "Processando atualização de cliente. CorrelationId: {CorrelationId} ClienteId: {ClienteId}",
            message.CorrelationId,
            message.ClienteId);

        // Reconstrói o DTO a partir dos dados da mensagem (separação entre contrato de fila e de domínio).
        var dto = new ClienteWriteDto
        {
            Nome = message.Nome,
            Email = message.Email,
            Documento = message.Documento
        };

        try
        {
            // Delega a atualização ao mesmo handler MediatR usado no fluxo síncrono (PUT /clientes/{id}).
            // Reutilizar UpdateClienteCommand garante consistência das regras de negócio
            // independente do caminho (síncrono ou assíncrono).
            var result = await _mediator.Send(new UpdateClienteCommand(message.ClienteId, dto));

            // Atualiza o estado da operação para Concluída, permitindo ao cliente confirmar
            // o sucesso via GET /operacoes/{correlationId}.
            if (result is not null)
                await MarcarOperacaoConcluidaAsync(message.CorrelationId, result.Id);
            _logger.LogInformation(
                "Cliente atualizado com sucesso. CorrelationId: {CorrelationId} ClienteId: {ClienteId}",
                message.CorrelationId,
                message.ClienteId);
        }
        catch (NotFoundException ex)
        {
            // Race condition: o cliente foi deletado entre a validação da API e o processamento aqui.
            // Como não há como recuperar desta situação, marcamos como falha (não adianta retry).
            await MarcarOperacaoFalhaAsync(message.CorrelationId, ex.Message);
            _logger.LogWarning(
                ex,
                "Cliente não encontrado no processamento assíncrono. CorrelationId: {CorrelationId} ClienteId: {ClienteId}",
                message.CorrelationId,
                message.ClienteId);
            // Re-throw: sinaliza ao Rebus a falha; comportamento de retry depende da configuração.
            throw;
        }
        catch (ConflictException ex)
        {
            // Race condition: outro cliente assumiu o e-mail ou documento antes deste processamento.
            // Falha de negócio não tem retry útil — marcamos como falha definitiva.
            await MarcarOperacaoFalhaAsync(message.CorrelationId, ex.Message);
            _logger.LogWarning(
                ex,
                "Conflito de negócio ao atualizar cliente. CorrelationId: {CorrelationId} ClienteId: {ClienteId}",
                message.CorrelationId,
                message.ClienteId);
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
