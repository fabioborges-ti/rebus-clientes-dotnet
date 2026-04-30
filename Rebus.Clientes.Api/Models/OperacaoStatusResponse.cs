namespace Rebus.Clientes.Api.Models;

public class OperacaoStatusResponse
{
    public Guid CorrelationId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public Guid? ClienteId { get; set; }
    public string? MensagemErro { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEmUtc { get; set; }
}
