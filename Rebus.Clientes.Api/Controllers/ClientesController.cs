using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rebus.Clientes.Api.Models;
using Rebus.Clientes.Application.Dtos;
using Rebus.Clientes.Application.Features.Clientes.Commands.DeleteCliente;
using Rebus.Clientes.Application.Features.Clientes.Commands.PublishCreateCliente;
using Rebus.Clientes.Application.Features.Clientes.Commands.PublishUpdateCliente;
using Rebus.Clientes.Application.Features.Clientes.Queries.GetClienteById;
using Rebus.Clientes.Application.Features.Clientes.Queries.GetClientes;
using Rebus.Clientes.Application.Features.Clientes.Queries.GetClientesPaged;

namespace Rebus.Clientes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public ClientesController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    /// <summary>
    /// Retorna a lista de todos os clientes cadastrados, ordenados do mais recente para o mais antigo.
    /// </summary>
    /// <response code="200">Lista retornada com sucesso (pode ser vazia).</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ClienteResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetClientesQuery(), cancellationToken);
        var data = result.Select(_mapper.Map<ClienteResponse>).ToList();

        return Ok(new ApiResponse<IReadOnlyList<ClienteResponse>>
        {
            Success = true,
            Message = "Clientes consultados com sucesso.",
            Errors = [],
            Data = data
        });
    }

    /// <summary>
    /// Retorna a lista de clientes de forma paginada.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="pageSize"></param>
    /// <response code="200">Página retornada com sucesso, incluindo objeto <c>paginacao</c> com totais.</response>
    /// <response code="400">Parâmetros inválidos (página menor que 1 ou tamanho fora do intervalo permitido).</response>
    [HttpGet("{page:int:min(1)}/{pageSize:int:min(1):max(100)}")]
    [ProducesResponseType(typeof(PagedResponse<ClienteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPaged(
        [FromRoute] int page = 1,
        [FromRoute] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetClientesPagedQuery(page, pageSize), cancellationToken);

        return Ok(new PagedResponse<ClienteResponse>
        {
            Success = true,
            Message = "Clientes consultados com sucesso.",
            Errors = [],
            Data = result.Items.Select(_mapper.Map<ClienteResponse>).ToList(),
            Paginacao = new PaginacaoMetadata
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalRegistros = result.TotalRegistros,
                TotalPaginas = result.TotalPaginas
            }
        });
    }

    /// <summary>
    /// Retorna os dados de um cliente específico pelo seu identificador único.
    /// </summary>
    /// <param name="id">Identificador único (GUID) do cliente.</param>
    /// <response code="200">Cliente encontrado e retornado com sucesso.</response>
    /// <response code="404">Nenhum cliente encontrado com o ID informado.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ClienteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetClienteByIdQuery(id), cancellationToken);

        return Ok(new ApiResponse<ClienteResponse>
        {
            Success = true,
            Message = "Cliente consultado com sucesso.",
            Errors = [],
            Data = _mapper.Map<ClienteResponse>(result)
        });
    }

    /// <summary>
    /// Solicita a criação de um novo cliente de forma assíncrona.
    /// A requisição é validada e, se aprovada, uma mensagem é publicada na fila para persistência pelo Worker.
    /// </summary>
    /// <remarks>
    /// O retorno <c>202 Accepted</c> indica que a solicitação foi recebida, mas ainda não persistida.
    /// Use o <c>CorrelationId</c> retornado para rastrear o processamento nos logs.
    /// </remarks>
    /// <response code="202">Solicitação recebida e enfileirada com sucesso.</response>
    /// <response code="400">Payload inválido ou e-mail já cadastrado.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateClienteAcceptedResponse>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Post([FromBody] CreateClienteRequest request, CancellationToken cancellationToken)
    {
        var correlationId = await _mediator.Send(
            new PublishCreateClienteCommand(_mapper.Map<ClienteWriteDto>(request)),
            cancellationToken);

        return Accepted(new ApiResponse<CreateClienteAcceptedResponse>
        {
            Success = true,
            Message = "Solicitação de criação recebida e enfileirada.",
            Errors = [],
            Data = new CreateClienteAcceptedResponse { CorrelationId = correlationId }
        });
    }

    /// <summary>
    /// Solicita a atualização dos dados de um cliente existente de forma assíncrona.
    /// As pré-condições (existência do cliente e unicidade do e-mail) são verificadas antes de enfileirar.
    /// </summary>
    /// <param name="id">Identificador único (GUID) do cliente a ser atualizado.</param>
    /// <response code="202">Solicitação de atualização recebida e enfileirada com sucesso.</response>
    /// <response code="400">Payload inválido.</response>
    /// <response code="404">Nenhum cliente encontrado com o ID informado.</response>
    /// <response code="409">O e-mail informado já pertence a outro cliente.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CreateClienteAcceptedResponse>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Put(Guid id, [FromBody] UpdateClienteRequest request, CancellationToken cancellationToken)
    {
        var correlationId = await _mediator.Send(
            new PublishUpdateClienteCommand(id, _mapper.Map<ClienteWriteDto>(request)),
            cancellationToken);

        return Accepted(new ApiResponse<CreateClienteAcceptedResponse>
        {
            Success = true,
            Message = "Solicitação de atualização recebida e enfileirada.",
            Errors = [],
            Data = new CreateClienteAcceptedResponse { CorrelationId = correlationId }
        });
    }

    /// <summary>
    /// Remove permanentemente um cliente pelo seu identificador único.
    /// A operação é síncrona: o cliente é excluído imediatamente do banco de dados.
    /// </summary>
    /// <param name="id">Identificador único (GUID) do cliente a ser removido.</param>
    /// <response code="200">Cliente removido com sucesso.</response>
    /// <response code="404">Nenhum cliente encontrado com o ID informado.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteClienteCommand(id), cancellationToken);

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Cliente removido com sucesso.",
            Errors = []
        });
    }
}
