using Rebus.Clientes.Application.Abstractions.Messaging;
using Rebus.Clientes.Application.Messaging;

namespace Rebus.Clientes.Worker.Messaging;

/// <summary>
/// Implementação nula de IClienteMessageBus para o Worker.
/// O Worker não publica mensagens — apenas consome da fila e persiste.
/// Este registro satisfaz o DI dos handlers PublishXxx registrados via AddApplication(),
/// que nunca são invocados no contexto do Worker.
/// </summary>
public sealed class NullClienteMessageBus : IClienteMessageBus
{
    public Task PublishAsync(CreateClienteMessage message, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublishAsync(UpdateClienteMessage message, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
