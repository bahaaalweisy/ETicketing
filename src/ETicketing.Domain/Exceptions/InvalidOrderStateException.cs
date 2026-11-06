namespace ETicketing.Domain.Exceptions;

public sealed class InvalidOrderStateException(string message) : DomainException(message);
