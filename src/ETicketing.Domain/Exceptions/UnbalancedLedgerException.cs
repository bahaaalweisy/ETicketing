namespace ETicketing.Domain.Exceptions;

/// <summary>
/// Debits and credits of a transaction differ, so the whole payment must roll back.
/// </summary>
public sealed class UnbalancedLedgerException(Guid transactionId, decimal debits, decimal credits)
    : DomainException($"Ledger for transaction {transactionId} is unbalanced: debits {debits}, credits {credits}.");
