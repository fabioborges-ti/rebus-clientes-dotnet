namespace Rebus.Clientes.Application.Abstractions.Correlation;

public interface ICorrelationIdAccessor
{
    Guid GetCorrelationId();
}
