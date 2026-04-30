namespace Rebus.Clientes.Api.Models;

public class CreateClienteAcceptedResponse
{
    public Guid CorrelationId { get; set; }
    public string Status { get; set; } = "Em processamento";
}
