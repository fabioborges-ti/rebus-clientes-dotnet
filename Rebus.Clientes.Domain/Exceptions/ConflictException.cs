namespace Rebus.Clientes.Domain.Exceptions;

public class ConflictException : DomainException
{
    public IReadOnlyList<string> Errors { get; }

    public ConflictException(string message) : base(message)
    {
        Errors = new List<string> { message };
    }

    public ConflictException(IEnumerable<string> errors) : base(string.Join("; ", errors))
    {
        Errors = errors.ToList();
    }
}
