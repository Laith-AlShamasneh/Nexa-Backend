namespace Domain.Exceptions;

public sealed class ForbiddenException : DomainException
{
    public ForbiddenException() : base("Access to this resource is forbidden.")
    {
    }

    public ForbiddenException(string message) : base(message)
    {
    }
}