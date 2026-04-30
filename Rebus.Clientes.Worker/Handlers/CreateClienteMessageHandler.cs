using MediatR;
using Rebus.Handlers;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Dtos;
using Rebus.Clientes.Application.Features.Clientes.Commands.CreateCliente;
using Rebus.Clientes.Application.Messaging;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.Worker.Handlers;

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

        var dto = new ClienteWriteDto
        {
            Nome = message.Nome,
            Email = message.Email,
            Documento = message.Documento
        };

        try
        {
            var result = await _mediator.Send(new CreateClienteCommand(dto));
            await MarcarOperacaoConcluidaAsync(message.CorrelationId, result.Id);
            _logger.LogInformation("Cliente persistido com sucesso. CorrelationId: {CorrelationId}", message.CorrelationId);
        }
        catch (ConflictException ex)
        {
            await MarcarOperacaoFalhaAsync(message.CorrelationId, ex.Message);
            _logger.LogWarning(ex, "Conflito de negócio ao criar cliente. CorrelationId: {CorrelationId}", message.CorrelationId);
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
