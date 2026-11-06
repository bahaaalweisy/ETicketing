namespace ETicketing.Domain.Exceptions;

public sealed class DuplicateEntryException(Exception innerException)
    : Exception("A unique index rejected the write.", innerException);
