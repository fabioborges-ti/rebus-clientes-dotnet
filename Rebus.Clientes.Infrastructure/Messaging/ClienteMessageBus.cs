using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;
using Rebus.Clientes.Application.Abstractions.Messaging;
using Rebus.Clientes.Application.Messaging;

namespace Rebus.Clientes.Infrastructure.Messaging;

/// <summary>
/// Implementação concreta (adapter) de <see cref="IClienteMessageBus"/> usando Rebus + RabbitMQ.
///
/// LAZY RESOLUTION — Por que não injetar IBus diretamente no construtor?
///   O Rebus registra o IBus como um IHostedService. Durante o startup, o DI container
///   valida todas as dependências (ValidateOnBuild), mas o IBus ainda não foi iniciado.
///   Injetar IBus diretamente causaria erro de resolução no startup.
///   A solução é receber IServiceProvider e resolver o IBus somente na primeira chamada,
///   momento em que o Rebus já está em execução.
///
/// Send() vs Publish() no Rebus:
///   Bus.Advanced.Routing.Send(queue, msg) → entrega direta a uma fila específica (ponto-a-ponto)
///   Bus.Publish(msg)                      → broadcast via exchange de tópicos (pub/sub)
///
///   Usamos Send() porque estamos enviando um COMANDO (intenção de executar uma ação),
///   não publicando um EVENTO (notificação de algo que já aconteceu).
///   Comandos têm um destinatário específico (o Worker); eventos têm múltiplos subscribers.
/// </summary>
public class ClienteMessageBus : IClienteMessageBus
{
    private readonly IServiceProvider _serviceProvider;

    // Nome da fila de destino — deve coincidir com o nome configurado no Worker (Program.cs).
    // O Rebus cria a fila no RabbitMQ automaticamente se ela não existir.
    private const string Queue = "clientes-commands-queue";

    public ClienteMessageBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // Resolução lazy do IBus via IServiceProvider.
    // Nota: Em versões recentes do Rebus.ServiceProvider, o IBus pode ser injetado
    // diretamente se o ValidateOnBuild estiver desativado. No entanto, esta abordagem
    // via IServiceProvider é a mais robusta para evitar problemas de circularidade
    // ou de inicialização do Rebus durante o startup, garantindo que o IBus
    // só seja resolvido quando realmente necessário (na primeira chamada de Send).
    // Isso é especialmente útil em cenários onde o Rebus é configurado como
    // IHostedService e pode não estar totalmente pronto no momento da validação do DI.
    private IBus Bus => _serviceProvider.GetRequiredService<IBus>();

    /// <summary>Envia o comando de criação para a fila via Rebus (Send ponto-a-ponto).</summary>
    public Task PublishAsync(CreateClienteMessage message, CancellationToken cancellationToken)
        => Bus.Advanced.Routing.Send(Queue, message);

    /// <summary>Envia o comando de atualização para a fila via Rebus (Send ponto-a-ponto).</summary>
    public Task PublishAsync(UpdateClienteMessage message, CancellationToken cancellationToken)
        => Bus.Advanced.Routing.Send(Queue, message);
}
