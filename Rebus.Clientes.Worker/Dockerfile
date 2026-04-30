FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Rebus.Clientes.slnx ./
COPY Rebus.Clientes.Worker/Rebus.Clientes.Worker.csproj Rebus.Clientes.Worker/
COPY Rebus.Clientes.Application/Rebus.Clientes.Application.csproj Rebus.Clientes.Application/
COPY Rebus.Clientes.Domain/Rebus.Clientes.Domain.csproj Rebus.Clientes.Domain/
COPY Rebus.Clientes.Infrastructure/Rebus.Clientes.Infrastructure.csproj Rebus.Clientes.Infrastructure/
RUN dotnet restore Rebus.Clientes.Worker/Rebus.Clientes.Worker.csproj

COPY . .
RUN dotnet publish Rebus.Clientes.Worker/Rebus.Clientes.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Rebus.Clientes.Worker.dll"]
