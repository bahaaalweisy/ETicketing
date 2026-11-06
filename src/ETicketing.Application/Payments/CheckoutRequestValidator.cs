using FluentValidation;

namespace ETicketing.Application.Payments;

/// <summary>
/// Validates a checkout: an order id and a defined payment method.
/// </summary>
public sealed class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutRequestValidator()
    {
        RuleFor(request => request.OrderId).NotEmpty();
        RuleFor(request => request.PaymentMethod).IsInEnum();
    }
}
