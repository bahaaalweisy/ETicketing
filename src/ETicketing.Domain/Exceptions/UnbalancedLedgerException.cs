namespace ETicketing.Domain.Exceptions;

public sealed class UnbalancedLedgerException(Guid transactionId, decimal debits, decimal credits)
    : DomainException($"Ledger for transaction {transactionId} is unbalanced: debits {debits}, credits {credits}.");
