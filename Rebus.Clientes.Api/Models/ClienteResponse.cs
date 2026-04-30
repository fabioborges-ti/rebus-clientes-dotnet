namespace Rebus.Clientes.Api.Models;

public class ClienteResponse
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}
