using Rebus.Clientes.Domain.Enums;

namespace Rebus.Clientes.Domain.Entities;

public class ClienteOperacao
{
    public Guid CorrelationId { get; private set; }
    public OperacaoTipo Tipo { get; private set; }
    public OperacaoEstado Estado { get; private set; }
    public Guid? ClienteId { get; private set; }
    public string? MensagemErro { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEmUtc { get; private set; }

    private ClienteOperacao()
    {
    }

    public ClienteOperacao(Guid correlationId, OperacaoTipo tipo, Guid? clienteId = null)
    {
        if (tipo == OperacaoTipo.Atualizacao && !clienteId.HasValue)
            throw new ArgumentException("Atualizacao requer o identificador do cliente alvo.", nameof(clienteId));

        CorrelationId = correlationId;
        Tipo = tipo;
        Estado = OperacaoEstado.Pendente;
        ClienteId = clienteId;
        CriadoEm = AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void MarcarConcluida(Guid clienteId)
    {
        Estado = OperacaoEstado.Concluida;
        ClienteId = clienteId;
        MensagemErro = null;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void MarcarFalha(string mensagem)
    {
        Estado = OperacaoEstado.Falhou;
        MensagemErro = mensagem;
        AtualizadoEmUtc = DateTime.UtcNow;
    }
}
