namespace Rebus.Clientes.Api.Models;

public class UpdateClienteRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
}
