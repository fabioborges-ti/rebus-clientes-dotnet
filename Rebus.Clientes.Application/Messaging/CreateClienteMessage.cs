namespace Rebus.Clientes.Application.Messaging;

/// <summary>
/// Contrato da mensagem publicada na fila do RabbitMQ quando uma criação de cliente é solicitada.
///
/// Esta classe representa o "envelope" que viaja pela fila de mensagens:
///   API (PublishCreateClienteCommandHandler) → RabbitMQ → Worker (CreateClienteMessageHandler)
///
/// O <see cref="CorrelationId"/> é gerado pelo handler da API antes de enfileirar e persiste
/// em <c>ClienteOperacao</c> (estado: Pendente). O Worker o utiliza para atualizar o status
/// ao concluir ou falhar, permitindo rastreamento de ponta a ponta via GET /operacoes/{correlationId}.
///
/// DICA: Mantenha mensagens simples (primitivos, GUIDs, strings). Evite referências
/// a entidades de domínio — mensagens precisam ser serializáveis/desserializáveis
/// independentemente de qualquer contexto de banco de dados ou domínio.
/// </summary>
public class CreateClienteMessage
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public DateTime EnfileiradoEmUtc { get; set; } = DateTime.UtcNow;
}
