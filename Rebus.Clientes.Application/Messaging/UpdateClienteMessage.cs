namespace Rebus.Clientes.Application.Messaging;

/// <summary>
/// Contrato da mensagem publicada na fila do RabbitMQ quando uma atualização de cliente é solicitada.
///
/// Esta classe representa o "envelope" que viaja pela fila de mensagens:
///   API (PublishUpdateClienteCommandHandler) → RabbitMQ → Worker (UpdateClienteMessageHandler)
///
/// O <see cref="ClienteId"/> identifica qual cliente será atualizado.
/// O <see cref="CorrelationId"/> é gerado pela API e persiste em <c>ClienteOperacao</c> (estado: Pendente),
/// permitindo ao consumidor da API consultar o resultado via GET /operacoes/{correlationId}.
///
/// DICA: Inclua apenas os dados necessários para o Worker executar a operação sem consultar
/// a API ou outros serviços — cada mensagem deve ser autocontida.
/// </summary>
public class UpdateClienteMessage
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public Guid ClienteId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public DateTime EnfileiradoEmUtc { get; set; } = DateTime.UtcNow;
}
