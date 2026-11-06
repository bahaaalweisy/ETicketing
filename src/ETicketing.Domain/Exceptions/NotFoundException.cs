namespace ETicketing.Domain.Exceptions;

public sealed class NotFoundException(string entityName, object id)
    : DomainException($"{entityName} '{id}' was not found.");
