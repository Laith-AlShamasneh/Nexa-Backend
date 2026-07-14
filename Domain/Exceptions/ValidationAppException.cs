namespace Domain.Exceptions;

public sealed class ValidationAppException(string message) : DomainException(message)
{
}