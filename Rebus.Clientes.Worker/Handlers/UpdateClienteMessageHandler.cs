using MediatR;
using Rebus.Handlers;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Dtos;
using Rebus.Clientes.Application.Features.Clientes.Commands.UpdateCliente;
using Rebus.Clientes.Application.Messaging;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.Worker.Handlers;

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

        var dto = new ClienteWriteDto
        {
            Nome = message.Nome,
            Email = message.Email,
            Documento = message.Documento
        };

        try
        {
            var result = await _mediator.Send(new UpdateClienteCommand(message.ClienteId, dto));
            if (result is not null)
                await MarcarOperacaoConcluidaAsync(message.CorrelationId, result.Id);
            _logger.LogInformation(
                "Cliente atualizado com sucesso. CorrelationId: {CorrelationId} ClienteId: {ClienteId}",
                message.CorrelationId,
                message.ClienteId);
        }
        catch (NotFoundException ex)
        {
            await MarcarOperacaoFalhaAsync(message.CorrelationId, ex.Message);
            _logger.LogWarning(
                ex,
                "Cliente não encontrado no processamento assíncrono. CorrelationId: {CorrelationId} ClienteId: {ClienteId}",
                message.CorrelationId,
                message.ClienteId);
            throw;
        }
        catch (ConflictException ex)
        {
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
