using Rebus.Clientes.Application.Messaging;

namespace Rebus.Clientes.Application.Abstractions.Messaging;

public interface IClienteMessageBus
{
    Task PublishAsync(CreateClienteMessage message, CancellationToken cancellationToken);
    Task PublishAsync(UpdateClienteMessage message, CancellationToken cancellationToken);
}
