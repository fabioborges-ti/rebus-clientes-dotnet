using Rebus.Clientes.Domain.Enums;

namespace Rebus.Clientes.Application.Dtos;

public class OperacaoStatusDto
{
    public Guid CorrelationId { get; set; }
    public OperacaoTipo Tipo { get; set; }
    public OperacaoEstado Estado { get; set; }
    public Guid? ClienteId { get; set; }
    public string? MensagemErro { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEmUtc { get; set; }
}
