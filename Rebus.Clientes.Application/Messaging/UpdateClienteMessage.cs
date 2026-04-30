namespace Rebus.Clientes.Application.Messaging;

public class UpdateClienteMessage
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public Guid ClienteId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public DateTime EnfileiradoEmUtc { get; set; } = DateTime.UtcNow;
}
