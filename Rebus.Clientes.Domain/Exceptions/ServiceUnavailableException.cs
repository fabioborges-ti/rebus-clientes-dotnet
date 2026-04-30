namespace Rebus.Clientes.Domain.Exceptions;

public class ServiceUnavailableException : DomainException
{
    public ServiceUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
