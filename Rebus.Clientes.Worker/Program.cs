// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  WORKER — Consumidor Rebus de mensagens de clientes                     ║
// ║                                                                          ║
// ║  Responsabilidade: consumir mensagens da fila "clientes-commands-queue"  ║
// ║  e executar as operações de escrita no banco de dados (PostgreSQL).      ║
// ║                                                                          ║
// ║  Este processo é o oposto da API:                                        ║
// ║    API    → publica mensagens na fila  (UseRabbitMqAsOneWayClient)       ║
// ║    Worker → consome mensagens da fila  (UseRabbitMq com nome de fila)    ║
// ╚══════════════════════════════════════════════════════════════════════════╝

using AutoMapper;
using Rebus.Clientes.Application;
using Rebus.Clientes.Application.Abstractions.Messaging;
using Rebus.Clientes.Application.Mapping;
using Rebus.Clientes.Infrastructure;
using Rebus.Clientes.Worker.Handlers;
using Rebus.Clientes.Worker.Messaging;
using Rebus.Config;
using Rebus.ServiceProvider;

var builder = Host.CreateApplicationBuilder(args);

// Registra toda a lógica de negócio (MediatR, validators) e infraestrutura (EF Core, repositórios).
// O Worker reutiliza as mesmas camadas da API — garantindo que as regras de negócio
// sejam aplicadas de forma idêntica independentemente do caminho (síncrono ou assíncrono).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// O Worker NÃO publica mensagens — apenas consome.
// NullClienteMessageBus implementa IClienteMessageBus sem fazer nada (Null Object Pattern).
// É necessário porque AddApplication() registra handlers Publish* que dependem de IClienteMessageBus,
// mas esses handlers nunca são chamados no contexto do Worker.
builder.Services.AddScoped<IClienteMessageBus, NullClienteMessageBus>();

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(ApplicationMappingProfile).Assembly));

// Descoberta automática de handlers Rebus no assembly do Worker.
// Qualquer classe que implemente IHandleMessages<T> é registrada automaticamente no DI.
// Atualmente: CreateClienteMessageHandler e UpdateClienteMessageHandler.
// Não é necessário registrá-los manualmente — basta implementar a interface.
builder.Services.AutoRegisterHandlersFromAssemblyOf<CreateClienteMessageHandler>();

var rabbitConnection = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException("Connection string 'RabbitMq' não encontrada.");

// Configura o Rebus como CONSUMIDOR da fila "clientes-commands-queue".
// UseRabbitMq() (com nome de fila) registra o Worker como consumidor ativo:
//   - Cria a fila no RabbitMQ se não existir
//   - Inicia um IHostedService que monitora a fila continuamente
//   - Despacha cada mensagem recebida para o handler correto pelo tipo da mensagem
//
// Diferença da API (UseRabbitMqAsOneWayClient): a API apenas envia, não consome,
// portanto não precisa de nome de fila nem de IHostedService de consumo.
builder.Services.AddRebus(configure => configure
    .Transport(t => t.UseRabbitMq(rabbitConnection, "clientes-commands-queue")));

var host = builder.Build();
host.Run();
