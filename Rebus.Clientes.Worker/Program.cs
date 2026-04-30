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
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IClienteMessageBus, NullClienteMessageBus>();
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(ApplicationMappingProfile).Assembly));

// O consumo das mensagens é feito pelo IHostedService do Rebus (registrado por AddRebus).
// Os handlers são descobertos automaticamente pelo assembly.
builder.Services.AutoRegisterHandlersFromAssemblyOf<CreateClienteMessageHandler>();

var rabbitConnection = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException("Connection string 'RabbitMq' não encontrada.");

builder.Services.AddRebus(configure => configure
    .Transport(t => t.UseRabbitMq(rabbitConnection, "clientes-commands-queue")));

var host = builder.Build();
host.Run();
