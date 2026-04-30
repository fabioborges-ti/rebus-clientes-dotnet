using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;
using Rebus.Clientes.Application.Abstractions.Messaging;
using Rebus.Clientes.Application.Messaging;

namespace Rebus.Clientes.Infrastructure.Messaging;

/// <summary>
/// Resolve o IBus do Rebus lazily para evitar falha na validação do DI container
/// durante o startup (ValidateOnBuild), pois o IBus só está disponível após
/// o IHostedService do Rebus ser iniciado.
/// </summary>
public class ClienteMessageBus : IClienteMessageBus
{
    private readonly IServiceProvider _serviceProvider;
    private const string Queue = "clientes-commands-queue";

    public ClienteMessageBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private IBus Bus => _serviceProvider.GetRequiredService<IBus>();

    public Task PublishAsync(CreateClienteMessage message, CancellationToken cancellationToken)
        => Bus.Advanced.Routing.Send(Queue, message);

    public Task PublishAsync(UpdateClienteMessage message, CancellationToken cancellationToken)
        => Bus.Advanced.Routing.Send(Queue, message);
}
