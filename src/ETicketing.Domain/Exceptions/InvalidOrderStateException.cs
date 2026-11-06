namespace ETicketing.Domain.Exceptions;

/// <summary>
/// The order is in a state that does not allow the requested action.
/// </summary>
public sealed class InvalidOrderStateException(string message) : DomainException(message);
