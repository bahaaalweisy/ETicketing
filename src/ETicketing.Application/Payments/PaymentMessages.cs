namespace ETicketing.Application.Payments;

public static class PaymentMessages
{
    public const string AlreadyInProgress = "This order already has a payment in progress or completed.";
    public const string OrderNoLongerPayable = "The order can no longer be paid.";
}
