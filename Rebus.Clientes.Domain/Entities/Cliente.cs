namespace Rebus.Clientes.Domain.Entities;

public class Cliente
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Documento { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }

    private Cliente()
    {
    }

    public Cliente(Guid id, string nome, string email, string documento)
    {
        Id = id;
        Atualizar(nome, email, documento);
        CriadoEm = DateTime.UtcNow;
    }

    public void Atualizar(string nome, string email, string documento)
    {
        Nome = nome.Trim();
        Email = email.Trim().ToLowerInvariant();
        Documento = documento.Trim();
    }
}
