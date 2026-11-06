namespace ETicketing.Domain.Exceptions;

/// <summary>
/// The payment transaction is in a state that does not allow the requested action.
/// </summary>
public sealed class InvalidTransactionStateException(string message) : DomainException(message);
