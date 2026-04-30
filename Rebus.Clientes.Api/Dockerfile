FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Rebus.Clientes.slnx ./
COPY Rebus.Clientes.Api/Rebus.Clientes.Api.csproj Rebus.Clientes.Api/
COPY Rebus.Clientes.Application/Rebus.Clientes.Application.csproj Rebus.Clientes.Application/
COPY Rebus.Clientes.Domain/Rebus.Clientes.Domain.csproj Rebus.Clientes.Domain/
COPY Rebus.Clientes.Infrastructure/Rebus.Clientes.Infrastructure.csproj Rebus.Clientes.Infrastructure/
RUN dotnet restore Rebus.Clientes.Api/Rebus.Clientes.Api.csproj

COPY . .
RUN dotnet publish Rebus.Clientes.Api/Rebus.Clientes.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Rebus.Clientes.Api.dll"]
