namespace ETicketing.Domain.Exceptions;

/// <summary>
/// The database rejected a write because of a unique index. It lets the inner layers
/// react to duplicates without knowing about the database provider.
/// </summary>
public sealed class DuplicateEntryException(Exception innerException)
    : Exception("A unique index rejected the write.", innerException);
