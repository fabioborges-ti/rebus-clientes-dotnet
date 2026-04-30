using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rebus.Clientes.Api.Models;
using Rebus.Clientes.Application.Features.Operacoes.Queries.GetOperacaoByCorrelationId;

namespace Rebus.Clientes.Api.Controllers;

[ApiController]
[Route("api/operacoes")]
public class OperacoesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public OperacoesController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    /// <summary>
    /// Consulta o estado de uma operação assíncrona (criação ou atualização) pelo identificador de correlação
    /// retornado no <c>202 Accepted</c> de <c>POST</c> / <c>PUT</c> em <c>/api/clientes</c>.
    /// </summary>
    /// <param name="correlationId">Identificador retornado no corpo da resposta assíncrona.</param>
    [HttpGet("{correlationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OperacaoStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCorrelationId(
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetOperacaoByCorrelationIdQuery(correlationId),
            cancellationToken);

        if (result is null)
        {
            return NotFound(new ApiResponse
            {
                Success = false,
                Message = "Operação não encontrada para o correlationId informado.",
                Errors = ["Operação não encontrada."]
            });
        }

        return Ok(new ApiResponse<OperacaoStatusResponse>
        {
            Success = true,
            Message = "Status da operação consultado com sucesso.",
            Errors = [],
            Data = _mapper.Map<OperacaoStatusResponse>(result)
        });
    }
}
