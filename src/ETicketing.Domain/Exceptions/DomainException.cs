namespace ETicketing.Domain.Exceptions;

/// <summary>
/// Base class for every failure that the business rules can raise on purpose.
/// </summary>
public abstract class DomainException(string message) : Exception(message);
