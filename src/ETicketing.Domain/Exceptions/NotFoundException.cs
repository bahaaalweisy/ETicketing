namespace ETicketing.Domain.Exceptions;

/// <summary>
/// The requested entity does not exist.
/// </summary>
public sealed class NotFoundException(string entityName, object id)
    : DomainException($"{entityName} '{id}' was not found.");
