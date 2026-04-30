using MediatR;
using Rebus.Clientes.Application.Abstractions.Persistence;
using Rebus.Clientes.Application.Dtos;

namespace Rebus.Clientes.Application.Features.Operacoes.Queries.GetOperacaoByCorrelationId;

public class GetOperacaoByCorrelationIdQueryHandler
    : IRequestHandler<GetOperacaoByCorrelationIdQuery, OperacaoStatusDto?>
{
    private readonly IClienteOperacaoRepository _repository;

    public GetOperacaoByCorrelationIdQueryHandler(IClienteOperacaoRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperacaoStatusDto?> Handle(
        GetOperacaoByCorrelationIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByCorrelationIdAsync(request.CorrelationId, cancellationToken);
        if (entity is null)
            return null;

        return new OperacaoStatusDto
        {
            CorrelationId = entity.CorrelationId,
            Tipo = entity.Tipo,
            Estado = entity.Estado,
            ClienteId = entity.ClienteId,
            MensagemErro = entity.MensagemErro,
            CriadoEm = entity.CriadoEm,
            AtualizadoEmUtc = entity.AtualizadoEmUtc
        };
    }
}
