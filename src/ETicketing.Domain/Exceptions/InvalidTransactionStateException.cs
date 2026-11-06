namespace ETicketing.Domain.Exceptions;

public sealed class InvalidTransactionStateException(string message) : DomainException(message);
