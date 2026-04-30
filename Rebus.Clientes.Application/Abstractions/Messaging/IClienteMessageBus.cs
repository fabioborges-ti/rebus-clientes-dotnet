using Rebus.Clientes.Application.Messaging;

namespace Rebus.Clientes.Application.Abstractions.Messaging;

/// <summary>
/// Porta de saída (output port) para envio de mensagens de comando de clientes à fila de mensagens.
///
/// ARQUITETURA — Por que esta interface vive na camada Application?
///   Clean Architecture proíbe que a camada Application dependa de detalhes de infraestrutura.
///   A solução é declarar aqui o contrato (port) e implementá-lo na Infrastructure (adapter):
///     Application  →  IClienteMessageBus           (abstração — sem referência ao Rebus)
///     Infrastructure → ClienteMessageBus            (implementação concreta com Rebus + RabbitMQ)
///     Worker         → NullClienteMessageBus        (Null Object — Worker não publica, apenas consome)
///
/// CONVENÇÃO DE NOME — Por que PublishAsync se internamente usa Send()?
///   O nome reflete a semântica do domínio ("publicar uma intenção").
///   A implementação usa Send() do Rebus (roteamento ponto-a-ponto para fila específica),
///   não Publish() (broadcast via exchange de tópicos para múltiplos subscribers).
/// </summary>
public interface IClienteMessageBus
{
    Task PublishAsync(CreateClienteMessage message, CancellationToken cancellationToken);
    Task PublishAsync(UpdateClienteMessage message, CancellationToken cancellationToken);
}
