namespace Domain.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException() : base("The requested resource was not found.")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}