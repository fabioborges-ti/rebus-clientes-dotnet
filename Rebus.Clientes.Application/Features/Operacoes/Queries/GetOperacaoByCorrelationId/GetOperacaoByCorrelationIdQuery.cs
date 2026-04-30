using MediatR;
using Rebus.Clientes.Application.Dtos;

namespace Rebus.Clientes.Application.Features.Operacoes.Queries.GetOperacaoByCorrelationId;

public record GetOperacaoByCorrelationIdQuery(Guid CorrelationId) : IRequest<OperacaoStatusDto?>;
